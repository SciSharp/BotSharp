using System.Text.Json.Serialization;

namespace BotSharp.Plugin.EmailHandler.LlmContexts;

public class LlmContextIn
{
    [JsonPropertyName("to_address")]
    public string? ToAddress { get; set; }

    [JsonPropertyName("email_content")]
    public string? Content { get; set; }

    [JsonPropertyName("subject")]
    public string? Subject { get; set; }

    [JsonPropertyName("is_need_attachments")]
    public bool IsNeedAttachemnts { get; set; }
}
