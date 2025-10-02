namespace BotSharp.Abstraction.CodeInterpreter.Models;

public class CodeInterpretResult
{
    public object Result { get; set; }
    public bool Success { get; set; }
    public string? ErrorMsg { get; set; }
}
