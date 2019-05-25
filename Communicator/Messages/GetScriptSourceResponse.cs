using Newtonsoft.Json;

namespace Communicator.Messages
{
    public class GetScriptSourceResponse
    {
        [JsonProperty("scriptSource")]
        public string ScriptSource { get; set; }
    }
}