using BotSharp.Abstraction.Tokenizers.Models;

namespace BotSharp.Abstraction.Tokenizers.Responses;

public class TokenizeResponse : ResponseBase
{
    public List<TokenizeResult> Results { get; set; } = [];
}
