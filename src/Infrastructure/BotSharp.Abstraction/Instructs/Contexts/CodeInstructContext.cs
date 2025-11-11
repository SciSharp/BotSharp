namespace BotSharp.Abstraction.Instructs.Contexts;

public class CodeInstructContext
{
    public AgentCodeScript CodeScript { get; set; }
    public List<KeyValue> Arguments { get; set; } = [];
}
