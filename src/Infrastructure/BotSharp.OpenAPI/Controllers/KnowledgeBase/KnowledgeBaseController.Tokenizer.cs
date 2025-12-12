using BotSharp.Abstraction.Tokenizers;
using BotSharp.Abstraction.Tokenizers.Responses;
using BotSharp.OpenAPI.ViewModels.Knowledges;

namespace BotSharp.OpenAPI.Controllers;

public partial class KnowledgeBaseController
{
    /// <summary>
    /// Tokenize text with options
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("knowledge/tokenize")]
    public async Task<TokenizeResponse?> Tokenize([FromBody] TokenizeRequest request)
    {
        var tokenizer = _services.GetServices<ITokenizer>()
                                 .FirstOrDefault(x => x.Provider.IsEqualTo(request.Provider));

        if (tokenizer == null)
        {
            return null;
        }
        return await tokenizer.TokenizeAsync(request.Text, request.Options);
    }

    /// <summary>
    /// Get tokenizer providers
    /// </summary>
    /// <returns></returns>
    [HttpGet("knowledge/tokenizer/provider")]
    public IEnumerable<string> GetTokenizerProviders()
    {
        var tokenizers = _services.GetServices<ITokenizer>();
        return tokenizers.Select(x => x.Provider);
    }

    /// <summary>
    /// Get token data loader providers
    /// </summary>
    /// <returns></returns>
    [HttpGet("knowledge/tokenizer/data-providers")]
    public IEnumerable<string> GetTokenizerDataProviders()
    {
        var dataLoaders = _services.GetServices<ITokenDataLoader>();
        return dataLoaders.Select(x => x.Provider);
    }
}
