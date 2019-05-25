using Newtonsoft.Json;

namespace Communicator.Messages
{
    public class RemoteObject
    {
        [JsonProperty("type")]
        public string ObjectType { get; set; }

        [JsonProperty("subtype")]
        public string ObjectSubtype { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        [JsonProperty("objectId")]
        public string ObjectId { get; set; }

        [JsonProperty("preview")]
        public object Preview { get; set; }
    }
}