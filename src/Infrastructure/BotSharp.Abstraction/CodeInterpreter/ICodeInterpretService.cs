using BotSharp.Abstraction.CodeInterpreter.Models;

namespace BotSharp.Abstraction.CodeInterpreter;

public interface ICodeInterpretService
{
    string Provider { get; }

    Task<CodeInterpretResult> RunCode(string codeScript, CodeInterpretOptions? options = null)
        => throw new NotImplementedException();
}
