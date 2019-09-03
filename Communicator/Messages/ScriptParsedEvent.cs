using Newtonsoft.Json;

namespace Communicator.Messages
{
    public class ScriptParsedEvent : IV8EventParameters
    {
        [JsonProperty("scriptId")]
        public string ScriptId { get; set; }

        [JsonProperty("startLine")]
        public int StartLine { get; set; }

        [JsonProperty("endLine")]
        public int EndLine { get; set; }

        [JsonProperty("startColumn")]
        public int StartColumn { get; set; }

        [JsonProperty("endColumn")]
        public int EndColumn { get; set; }

        // More properties e.g. relating to source mapping
        // ...

        [JsonProperty("length")]
        public int Length { get; set; }
    }
}