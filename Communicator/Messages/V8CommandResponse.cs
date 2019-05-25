using Newtonsoft.Json;

namespace Communicator.Messages
{
    public class V8CommandResponse<T> : V8CommandResponse where T : new()
    {
        [JsonProperty("result")]
        public T Result { get; set; }
    }

    public class V8CommandResponse
    {
        [JsonProperty("id")]
        [JsonRequired]
        public int RequestId { get; set; }
    }
}