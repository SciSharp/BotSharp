namespace BotSharp.Abstraction.Coding.Responses;

public class CodeInterpretResponse : ResponseBase
{
    public string Result { get; set; } = string.Empty;

    public override string ToString()
    {
        return Result ?? ErrorMsg ?? $"Success: {Success}";
    }
}
