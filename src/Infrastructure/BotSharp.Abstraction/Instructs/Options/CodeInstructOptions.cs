namespace BotSharp.Abstraction.Instructs.Options;

public class CodeInstructOptions
{
    public string? Processor { get; set; }
    public string? CodeScriptName { get; set; }
    public List<KeyValue>? Arguments { get; set; }
}
