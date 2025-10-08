using System.Threading;

namespace BotSharp.Abstraction.CodeInterpreter.Models;

public class CodeInterpretOptions
{
    public IEnumerable<KeyValue>? Arguments { get; set; }
    public bool LockFree { get; set; }
    public CancellationToken? CancellationToken { get; set; }
}
