namespace BotSharp.Abstraction.Instructs.Options;

public class CodeInstructOptions
{
    public string? CodeScriptName { get; set; }
    public string? CodeInterpretProvider { get; set; }
    public List<KeyValue>? Arguments { get; set; }
}
