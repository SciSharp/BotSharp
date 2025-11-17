using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Instructs;

public class InstructBaseViewModel : ResponseBase
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
