using System.Text.Json.Serialization;

namespace BotSharp.Plugin.KnowledgeBase.LlmContexts;

public class KnowledgeContextIn
{
    [JsonPropertyName("question")]
    public string Question { get; set; }
}
