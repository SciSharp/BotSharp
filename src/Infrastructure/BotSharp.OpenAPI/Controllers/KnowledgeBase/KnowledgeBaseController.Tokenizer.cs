using BotSharp.Abstraction.Tokenizers;
using BotSharp.Abstraction.Tokenizers.Responses;
using BotSharp.OpenAPI.ViewModels.Knowledges;

namespace BotSharp.OpenAPI.Controllers;

public partial class KnowledgeBaseController
{
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
}
