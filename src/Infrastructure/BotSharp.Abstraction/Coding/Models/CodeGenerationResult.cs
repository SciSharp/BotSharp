namespace BotSharp.Abstraction.Coding.Models;

public class CodeGenerationResult : ResponseBase
{
    public string Content { get; set; }
    public string Language { get; set; }
}
