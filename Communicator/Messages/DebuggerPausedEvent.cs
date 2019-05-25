using Newtonsoft.Json;

namespace Communicator.Messages
{
    public class DebuggerPausedEvent
    {
        [JsonProperty("callFrames")]
        public CallFrame[] CallFrames { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }
    }
}