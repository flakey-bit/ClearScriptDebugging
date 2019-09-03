using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Communicator.Messages;
using Newtonsoft.Json;

namespace Communicator
{
    internal class MessagePump
    {
        private readonly ClientWebSocket _socket;
        private readonly IV8EventHandler _events;

        public MessagePump(ClientWebSocket socket, IV8EventHandler events)
        {
            _socket = socket;
            _events = events;
        }

        public async Task Run(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                string json = await Receive(token);
                HandleMessage(json);
            }
        }

        private async Task<string> Receive(CancellationToken token)
        {
            List<byte> bytes = new List<byte>();

            WebSocketReceiveResult receiveResult;
            do
            {
                var buffer = new ArraySegment<byte>(new byte[1024 * 8]);
                receiveResult = await _socket.ReceiveAsync(buffer, token);
                bytes.AddRange(buffer);
            } while (!receiveResult.EndOfMessage);

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        private void HandleMessage(string json)
        {
            // FIXME: This is super rough.
            try
            {
                var commandResponse = JsonConvert.DeserializeObject<V8CommandResponse>(json);
                _events.Raise(new CommandResponse(commandResponse.RequestId, json));
                return;
            }
            catch (JsonSerializationException)
            {
                // Not a command response
            }

            if (json.Contains("Debugger.paused"))
            {
                var debuggerPausedEvent = JsonConvert.DeserializeObject<V8DebuggerEvent<DebuggerPausedEvent>>(json);
                _events.Raise(debuggerPausedEvent.EventParameters);
            } else if (json.Contains("scriptParsed"))
            {
                var scriptDetails = JsonConvert.DeserializeObject<V8DebuggerEvent<ScriptParsedEvent>>(json);
                _events.Raise(scriptDetails.EventParameters);
            }
        }
    }
}