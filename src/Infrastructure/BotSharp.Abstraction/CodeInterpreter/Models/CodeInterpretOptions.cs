using System.Threading;

namespace BotSharp.Abstraction.CodeInterpreter.Models;

public class CodeInterpretOptions
{
    public string? ScriptName { get; set; }
    public IEnumerable<KeyValue>? Arguments { get; set; }
    public CancellationToken? CancellationToken { get; set; }
}
