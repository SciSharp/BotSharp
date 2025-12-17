using BotSharp.Abstraction.Tokenizers.Models;
using BotSharp.Abstraction.Tokenizers.Responses;

namespace BotSharp.Abstraction.Tokenizers;

public interface ITokenizer
{
    string Provider { get; }

    Task<TokenizeResponse> TokenizeAsync(string text, TokenizeOptions? options = null);
}
