namespace BotSharp.Abstraction.CodeInterpreter.Models;

public class CodeInterpretResult
{
    public string Result { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMsg { get; set; }
}
