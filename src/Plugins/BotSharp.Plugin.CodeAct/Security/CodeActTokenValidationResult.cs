namespace BotSharp.Plugin.CodeAct.Security;

public class CodeActTokenValidationResult
{
    public bool Success { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public static CodeActTokenValidationResult Ok()
    {
        return new CodeActTokenValidationResult
        {
            Success = true,
            ReasonCode = "codeact.token_valid",
            Message = "Token is valid."
        };
    }

    public static CodeActTokenValidationResult Fail(string reasonCode, string message)
    {
        return new CodeActTokenValidationResult
        {
            Success = false,
            ReasonCode = reasonCode,
            Message = message
        };
    }
}
