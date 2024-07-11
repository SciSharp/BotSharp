using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EmailHandler.LlmContexts
{
    public class LlmContextIn
    {
        [JsonPropertyName("to_address")]
        public string? ToAddress { get; set; }

        [JsonPropertyName("email_content")]
        public string? Content { get; set; }
        [JsonPropertyName("subject")]
        public string? Subject { get; set; }
    }
}
