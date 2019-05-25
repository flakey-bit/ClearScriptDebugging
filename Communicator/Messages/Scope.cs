using Newtonsoft.Json;

namespace Communicator.Messages
{
    public class Scope
    {
        [JsonProperty("type")]
        public string ScopeType { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("object")]
        public RemoteObject Object { get; set; }
    }
}