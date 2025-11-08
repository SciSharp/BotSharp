using System.Threading;

namespace BotSharp.Abstraction.Coding.Options;

public class CodeInterpretOptions
{
    public string? ScriptName { get; set; }
    public IEnumerable<KeyValue>? Arguments { get; set; }
    public bool UseLock { get; set; }
    public bool UseProcess { get; set; }
    public CancellationToken? OperationToken { get; set; }
    public CancellationToken? LockToken { get; set; }
}
