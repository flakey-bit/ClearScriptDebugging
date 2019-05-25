using Newtonsoft.Json;

namespace Communicator.Messages
{
    public class EvaluateOnCallFrameResponse
    {
        [JsonProperty("result")]
        public RemoteObject Result { get; set; }
    }
}