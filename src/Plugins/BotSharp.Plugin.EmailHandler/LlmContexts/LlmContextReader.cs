using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EmailHandler.LlmContexts
{
    public class LlmContextReader
    {
        [JsonPropertyName("mark_as_read")]
        public bool? IsMarkRead { get; set; }
        [JsonPropertyName("message_id")]
        public string? MessageId { get; set; }
        [JsonPropertyName("is_email_summarize")]
        public bool? IsSummarize { get; set; }
    }
}
