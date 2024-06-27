using System.Text.Json.Serialization;

namespace BotSharp.Plugin.AzureOpenAI.Models;

public class TextCompletionResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("choices")]
    public IEnumerable<TexCompletionChoice> Choices { get; set; } = new List<TexCompletionChoice>();

    [JsonPropertyName("usage")]
    public TexCompletionUsage Usage { get; set; }
}

public class TexCompletionChoice
{
    [JsonPropertyName("text")]
    public string Text { get; set; }

    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}

public class TexCompletionUsage
{
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}