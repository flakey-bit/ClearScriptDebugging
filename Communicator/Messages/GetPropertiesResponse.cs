using Newtonsoft.Json;

namespace Communicator.Messages
{
    public class GetPropertiesResponse
    {
        [JsonProperty("result")]
        public PropertyDescriptor[] PropertyDescriptors { get; set; }
    }
}