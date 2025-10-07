namespace BotSharp.Abstraction.Instructs.Models;

public class CodeInstructOptions
{
    public string? CodeScriptName { get; set; }
    public string? CodeInterpretProvider { get; set; }
    public List<KeyValue>? Arguments { get; set; }
}
