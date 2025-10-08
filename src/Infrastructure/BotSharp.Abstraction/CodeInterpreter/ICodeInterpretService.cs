using BotSharp.Abstraction.CodeInterpreter.Models;
using System.Threading;

namespace BotSharp.Abstraction.CodeInterpreter;

public interface ICodeInterpretService
{
    string Provider { get; }

    Task<CodeInterpretResult> RunCode(string codeScript, CodeInterpretOptions? options = null)
        => throw new NotImplementedException();
}
