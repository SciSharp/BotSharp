namespace BotSharp.Abstraction.Coding.Contexts;

public class CodeExecutionContext
{
    public AgentCodeScript CodeScript { get; set; }
    public List<KeyValue> Arguments { get; set; } = [];
}
