namespace BotSharp.Abstraction.Instructs.Models;

public class CodeInstructContext
{
    public string CodeScript { get; set; }
    public List<KeyValue> Arguments { get; set; } = [];
}
