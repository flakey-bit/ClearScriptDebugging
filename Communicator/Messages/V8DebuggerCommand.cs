using Newtonsoft.Json;

namespace Communicator.Messages
{
    public class V8DebuggerCommand
    {
        [JsonProperty("id")]
        [JsonRequired]
        public int Id { get; set; }

        [JsonProperty("method")]
        [JsonRequired]
        public string MethodName { get; set; }

        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public object Parameters { get; set; }
    }
}