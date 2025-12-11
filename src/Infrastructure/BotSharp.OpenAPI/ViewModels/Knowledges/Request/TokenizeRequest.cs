using BotSharp.Abstraction.Tokenizers.Models;

namespace BotSharp.OpenAPI.ViewModels.Knowledges;

public class TokenizeRequest
{
    public string Text { get; set; } = string.Empty;
    public string? Provider { get; set; } = "fuzzy-sharp";
    public TokenizeOptions? Options { get; set; }
}
