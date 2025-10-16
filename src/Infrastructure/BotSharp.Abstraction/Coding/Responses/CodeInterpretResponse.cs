namespace BotSharp.Abstraction.Coding.Responses;

public class CodeInterpretResponse
{
    public string Result { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMsg { get; set; }
}
