namespace BotSharp.Abstraction.Coding.Contexts;

public class CodeExecutionContext
{
    public AgentCodeScript CodeScript { get; set; }
    public IEnumerable<KeyValue> Arguments { get; set; } = [];
}
