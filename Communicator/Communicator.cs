using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Communicator.Messages;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Communicator
{
    public class Communicator : ICommunicator, IV8EventHandler, IDisposable
    {
        private readonly string _hostname;
        private readonly int _port;

        private readonly Dictionary<Type, Queue<object>> _eventBuffer;
        private readonly object _eventBufferLock = new object();
        private readonly Dictionary<Type, Queue<object>> _taskCompletionSourcesLookup;
        private readonly object _taskCompletionSourcesLock = new object();

        private ClientWebSocket _socket;
        private int _seq;
        private CancellationTokenSource _messagePumpTokenSource;

        public Communicator(string hostname, int port)
        {
            _hostname = hostname;
            _port = port;

            _eventBuffer = new Dictionary<Type, Queue<object>>();
            _taskCompletionSourcesLookup = new Dictionary<Type, Queue<object>>();
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            var tabInfo = await GetTabInfo(_hostname, _port);
            _socket = new ClientWebSocket();
            await _socket.ConnectAsync(new Uri($"ws://{_hostname}:{_port}/{tabInfo.Id}"), cancellationToken);
            _messagePumpTokenSource = StartMessagePump();
        }

        public async Task<string> SendCommand(string method, object parameters = null)
        {
            EnsureConnected();

            V8DebuggerCommand payload = new V8DebuggerCommand
            {
                Id = _seq++,
                MethodName = method,
                Parameters = parameters
            };

            string jsonPayload = JsonConvert.SerializeObject(payload);

            var bytes = Encoding.UTF8.GetBytes(jsonPayload);

            await _socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);

            var commandCompletedEvent = await WaitForEventAsync<CommandResponse>(CancellationToken.None);
            if (payload.Id != commandCompletedEvent.RequestId)
            {
                throw new Exception("Got unexpected command result back");
            }

            return commandCompletedEvent.RawJson;
        }

        public Task<TEvent> WaitForEventAsync<TEvent>(CancellationToken token) where TEvent : IV8EventParameters
        {
            var eventType = typeof(TEvent);

            // Check for a buffered event, if so use that
            lock (_eventBufferLock)
            {
                if (_eventBuffer.TryGetValue(eventType, out var queuedEvents) && queuedEvents.TryDequeue(out var queuedEvent))
                {
                    return Task.FromResult((TEvent)queuedEvent);
                }
            }

            EnsureConnected();

            // Create a task completion source for the event
            var tcs = new TaskCompletionSource<TEvent>(token, TaskCreationOptions.RunContinuationsAsynchronously);

            lock (_taskCompletionSourcesLock)
            {
                if (!_taskCompletionSourcesLookup.TryGetValue(eventType, out var sources))
                {
                    sources = _taskCompletionSourcesLookup[eventType] = new Queue<object>();
                }
                sources.Enqueue(tcs);
            }

            return tcs.Task;
        }

        public async Task Disconnect(CancellationToken cancellationToken)
        {
            try
            {
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
            }
            catch (Exception)
            {
                // Ignore
            }

            try
            {
                _messagePumpTokenSource?.Cancel();
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        #region Implementation of IV8EventHandler

        // Called from the message pump thread
        public void Raise<TEvent>(TEvent e) where TEvent : IV8EventParameters
        {
            var eventType = typeof(TEvent);

            if (CheckEventTaskCompletion(e))
            {
                return;
            }

            lock (_eventBufferLock)
            {
                if (!_eventBuffer.TryGetValue(eventType, out var eventsOfType))
                {
                    eventsOfType = _eventBuffer[eventType] = new Queue<object>();
                }

                eventsOfType.Enqueue(e);
            }
        }

        #endregion

        private bool CheckEventTaskCompletion<TEvent>(TEvent e)
        {
            var eventType = typeof(TEvent);

            lock (_taskCompletionSourcesLock)
            {
                if (_taskCompletionSourcesLookup.TryGetValue(eventType, out var waitingTasks) && waitingTasks.TryDequeue(out var tcs))
                {
                    var taskCompletionSource = (TaskCompletionSource<TEvent>)tcs;
                    taskCompletionSource.SetResult(e);
                    return true;
                }

                return false;
            }
        }

        private void EnsureConnected()
        {
            if (_socket == null)
            {
                throw new InvalidOperationException("Not connected");
            }
        }

        private static async Task<TabInfo> GetTabInfo(string hostname, int port)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri($"http://{hostname}:{port}");

            var request = new HttpRequestMessage(HttpMethod.Get, "/json/list");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<TabInfo[]>(content)[0];
        }

        private CancellationTokenSource StartMessagePump()
        {
            var cancellationTokenSource = new CancellationTokenSource();
            var eventPump = new MessagePump(_socket, this);
            new Task(() => eventPump.Run(cancellationTokenSource.Token), cancellationTokenSource.Token, TaskCreationOptions.LongRunning).Start();
            return cancellationTokenSource;
        }

        public void Dispose()
        {
            _socket?.Dispose();
            _messagePumpTokenSource?.Dispose();
        }
    }
}