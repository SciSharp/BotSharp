using BotSharp.Abstraction.Coding.Options;
using BotSharp.Abstraction.Coding.Responses;

namespace BotSharp.Abstraction.Coding;

public interface ICodeProcessor
{
    string Provider { get; }

    Task<CodeInterpretResponse> RunAsync(string codeScript, CodeInterpretOptions? options = null)
        => throw new NotImplementedException();
}
