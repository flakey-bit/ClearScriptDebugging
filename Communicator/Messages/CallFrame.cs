using Newtonsoft.Json;

namespace Communicator.Messages
{
    public class CallFrame
    {
        [JsonProperty("callFrameId")]
        public string CallFrameId { get; set; }

        [JsonProperty("functionName")]
        public string FunctionName { get; set; }

        [JsonProperty("scopeChain")]
        public Scope[] ScopeChain { get; set; }
    }
}