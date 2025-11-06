namespace BotSharp.Abstraction.Instructs.Contexts;

public class CodeInstructContext
{
    public string ScriptName { get; set; }
    public string ScriptContent { get; set; }
    public string ScriptType { get; set; }
    public List<KeyValue> Arguments { get; set; } = [];
}
