using Communicator.Messages;

namespace Communicator
{
    public class V8EngineEvents
    {
        public delegate void HandlerForDebuggerPausedEvent(object sender, DebuggerPausedEvent e);
        public event HandlerForDebuggerPausedEvent DebuggerPausedEventHandler;

        public delegate void HandlerForCommandCompleted(object sender, CommandResponse e);
        public event HandlerForCommandCompleted CommandCompletedEventHandler;

        public delegate void HandlerForScriptParsed(object sender, ScriptParsedEvent e);
        public event HandlerForScriptParsed ScriptParsedEventHandler;

        public void RaiseDebuggerPausedEvent(DebuggerPausedEvent debuggerPausedEvent)
        {
            DebuggerPausedEventHandler?.Invoke(this, debuggerPausedEvent);
        }

        public void RaiseScriptParsedEvent(ScriptParsedEvent scriptParsedEvent)
        {
            ScriptParsedEventHandler?.Invoke(this, scriptParsedEvent);
        }

        public void RaiseCommandCompletedEvent(CommandResponse commandResponse)
        {
            CommandCompletedEventHandler?.Invoke(this, commandResponse);
        }
    }
}