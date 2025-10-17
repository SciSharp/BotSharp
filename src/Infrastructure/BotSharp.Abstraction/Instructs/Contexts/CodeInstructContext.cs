namespace BotSharp.Abstraction.Instructs.Contexts;

public class CodeInstructContext
{
    public string CodeScript { get; set; }
    public List<KeyValue> Arguments { get; set; } = [];
}
