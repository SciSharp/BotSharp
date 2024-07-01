using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Instructs;

public class PdfCompletionViewModel
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }
}
