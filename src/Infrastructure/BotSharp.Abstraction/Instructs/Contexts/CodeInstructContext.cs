namespace BotSharp.Abstraction.Instructs.Contexts;

public class CodeInstructContext
{
    public string CodeScript { get; set; }
    public string ScriptType { get; set; }
    public List<KeyValue> Arguments { get; set; } = [];
}
