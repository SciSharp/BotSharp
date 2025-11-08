using System.Threading;

namespace BotSharp.Abstraction.Coding.Options;

public class CodeInterpretOptions
{
    public string? ScriptName { get; set; }
    public IEnumerable<KeyValue>? Arguments { get; set; }
    public bool UseMutex { get; set; }
    public bool UseProcess { get; set; }
    public CancellationToken? CancellationToken { get; set; }
}
