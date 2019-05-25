using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Communicator.Messages;
using Newtonsoft.Json;

namespace Communicator
{
    public class Communicator : IDisposable
    {
        private readonly V8EngineEvents _events;
        private readonly string _hostname;
        private readonly int _port;
        private readonly Channel<DebuggerPausedEvent> _debuggerPausedEventChannel;
        private readonly Channel<CommandResponse> _commandCompletedEventChannel;
        private readonly Channel<ScriptParsedEvent> _scriptParsedEventChannel;

        private ClientWebSocket _socket;
        private int _seq;
        private CancellationTokenSource _messagePumpTokenSource;

        public Communicator(string hostname, int port)
        {
            _hostname = hostname;
            _port = port;
            _events = new V8EngineEvents();

            _debuggerPausedEventChannel = Channel.CreateUnbounded<DebuggerPausedEvent>();
            _commandCompletedEventChannel = Channel.CreateUnbounded<CommandResponse>();
            _scriptParsedEventChannel = Channel.CreateUnbounded<ScriptParsedEvent>();

            _events.DebuggerPausedEventHandler += (sender, eventDetails) =>
            {
                _debuggerPausedEventChannel.Writer.TryWrite(eventDetails);
            };

            _events.CommandCompletedEventHandler += (sender, eventDetails) =>
            {
                _commandCompletedEventChannel.Writer.TryWrite(eventDetails);
            };

            _events.ScriptParsedEventHandler += (sender, eventDetails) =>
            {
                _scriptParsedEventChannel.Writer.TryWrite(eventDetails);
            };
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

            var commandCompletedEvent = await _commandCompletedEventChannel.Reader.ReadAsync(CancellationToken.None);
            if (payload.Id != commandCompletedEvent.RequestId)
            {
                throw new Exception("Got unexpected command result back");
            }

            return commandCompletedEvent.RawJson;
        }

        public async Task<DebuggerPausedEvent> WaitForDebuggerPausedEventAsync(CancellationToken token)
        {
            EnsureConnected();
            return await _debuggerPausedEventChannel.Reader.ReadAsync(token);
        }

        public async Task<ScriptParsedEvent> WaitForScriptParsedEventAsync(CancellationToken token)
        {
            EnsureConnected();
            return await _scriptParsedEventChannel.Reader.ReadAsync(token);
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
            var eventPump = new MessagePump(_socket, _events);
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