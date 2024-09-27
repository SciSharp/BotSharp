using System.Text.Json.Serialization;

namespace BotSharp.Plugin.ExcelHandler.LlmContexts
{
    public class LlmContextIn
    {
        [JsonPropertyName("user_request")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public string? UserRequest { get; set; }

        [JsonPropertyName("is_need_processing")]
        public bool IsNeedProcessing { get; set; }
    }
}
