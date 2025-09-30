using BotSharp.Abstraction.CodeInterpreter.Models;

namespace BotSharp.Abstraction.CodeInterpreter;

public interface ICodeInterpretService
{
    string Provider { get; }

    Task<CodeInterpretResult> RunCode(string codeScript, IEnumerable<KeyValue>? arguments = null, CodeInterpretOptions? options = null);
}
