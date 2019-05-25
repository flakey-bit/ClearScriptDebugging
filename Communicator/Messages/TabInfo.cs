using Newtonsoft.Json;

namespace Communicator.Messages
{
    public class TabInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}