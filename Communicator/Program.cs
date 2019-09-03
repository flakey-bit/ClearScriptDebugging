using System;
using System.Threading;
using System.Threading.Tasks;
using Communicator.Messages;
using Newtonsoft.Json;

namespace Communicator
{
    public class Program
    {
        private static readonly TimeSpan WaitForDebuggerTimeout = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan WebsocketConnectTimeout = TimeSpan.FromSeconds(10);

        public static async Task Main()
        {
            using (var communicator = new Communicator("localhost", 8881))
            {
                await communicator.Connect(new CancellationTokenSource(WebsocketConnectTimeout).Token);

                // https://chromedevtools.github.io/devtools-protocol/1-2/Debuggerd

                await communicator.SendCommand("Runtime.enable");
                await communicator.SendCommand("Debugger.enable");

                // First script loaded is from the ClearScript V8ScriptEngine - contains helper functions such as invokeMethod, getStackTrace etc
                // We don't see that script until we send the Debugger.enable command
                await communicator.WaitForEventAsync<ScriptParsedEvent>(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);

                // We need to tell V8 that we've assumed they responsibility of the debugger it was waiting for. If we didn't do that
                // and closed the websocket, the V8 process would remain halted.
                await communicator.SendCommand("Runtime.runIfWaitingForDebugger");

                // After resuming the script (runIfWaitingForDebugger) we should get both a ScriptParsed event & a DebuggerPaused event.
                // This second script is the one we're actually debugging (i.e. the one the 'debugger' statement in it)
                var scriptParsedEvent = await communicator.WaitForEventAsync<ScriptParsedEvent>(new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
                var commandResult = await communicator.SendCommand("Debugger.getScriptSource", new {scriptId = scriptParsedEvent.ScriptId});
                var scriptSourceResponse = JsonConvert.DeserializeObject<V8CommandResponse<GetScriptSourceResponse>>(commandResult);
                Console.Out.WriteLine($"Debugging script: {scriptSourceResponse.Result.ScriptSource}");

                // Break on debugger connection (as requested)
                await communicator.WaitForEventAsync<DebuggerPausedEvent>(new CancellationTokenSource(WaitForDebuggerTimeout).Token);

                Console.Out.WriteLine("Resuming debugger");
                await communicator.SendCommand("Debugger.resume");

                // Break on our user breakpoint (debugger keyword)
                var debuggerPausedEvent = await communicator.WaitForEventAsync<DebuggerPausedEvent>(new CancellationTokenSource(WaitForDebuggerTimeout).Token);

                // Pull out the callFrameId for that frame
                string callFrameId = debuggerPausedEvent.CallFrames[0].CallFrameId;
                // Pull out a reference to the global object id (top scope of that call frame)
                string globalObjectId = debuggerPausedEvent.CallFrames[0].ScopeChain[0].Object.ObjectId;

                // Evaluate the variable 'two' (should be undefined)
                commandResult = await communicator.SendCommand("Debugger.evaluateOnCallFrame", new {callFrameId, expression = "two"});
                var evaluateResult = JsonConvert.DeserializeObject<V8CommandResponse<EvaluateOnCallFrameResponse>>(commandResult);
                Console.Out.WriteLine($"Result from evaluating expression 'two' before setVariableValue: type: {evaluateResult.Result.Result.ObjectType}, value: {evaluateResult.Result.Result.Value}");

                // Set the value of the variable 'two'. Note that setting a variable value will cause a scriptParsed event to be emitted, as will evaluating on call frame (below)
                await communicator.SendCommand("Debugger.setVariableValue", new {scopeNumber = 0, variableName = "two", newValue = new {value = 3}, callFrameId});

                // Evaluate the variable 'two' again (should be 3)
                commandResult = await communicator.SendCommand("Debugger.evaluateOnCallFrame", new {callFrameId, expression = "two"});
                evaluateResult = JsonConvert.DeserializeObject<V8CommandResponse<EvaluateOnCallFrameResponse>>(commandResult);
                Console.Out.WriteLine($"Result from evaluating expression 'two' after setVariableValue: type: {evaluateResult.Result.Result.ObjectType}, value: {evaluateResult.Result.Result.Value}");

                // Get the global object scope
                commandResult = await communicator.SendCommand("Runtime.getProperties",
                    new
                    {
                        accessorPropertiesOnly = false, generatePreview = true, objectId = globalObjectId,
                        ownProperties = false
                    });
                var objectProperties = JsonConvert.DeserializeObject<V8CommandResponse<GetPropertiesResponse>>(commandResult);

                await communicator.Disconnect(new CancellationTokenSource(WebsocketConnectTimeout).Token);
            }
        }
    }
}
