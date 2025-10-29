using BotSharp.Abstraction.Coding.Models;
using BotSharp.Abstraction.Coding.Options;
using BotSharp.Abstraction.Coding.Responses;

namespace BotSharp.Abstraction.Coding;

public interface ICodeProcessor
{
    string Provider { get; }

    /// <summary>
    /// Run code script
    /// </summary>
    /// <param name="codeScript">The code scirpt to run</param>
    /// <param name="options">Code script execution options</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    Task<CodeInterpretResponse> RunAsync(string codeScript, CodeInterpretOptions? options = null)
        => throw new NotImplementedException();

    /// <summary>
    /// Generate code script
    /// </summary>
    /// <param name="text">User requirement to generate code script</param>
    /// <param name="options">Code script generation options</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    Task<CodeGenerationResult> GenerateCodeScriptAsync(string text, CodeGenerationOptions? options = null)
        => throw new NotImplementedException();
}
