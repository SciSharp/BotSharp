namespace BotSharp.Plugin.CodeAct.Security;

public class CodeActDecision
{
    public bool Allowed { get; set; }
    public bool ApprovalRequired { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public static CodeActDecision Allow(string reasonCode = "codeact.allowed", string message = "Allowed")
    {
        return new CodeActDecision
        {
            Allowed = true,
            ReasonCode = reasonCode,
            Message = message
        };
    }

    public static CodeActDecision Deny(string reasonCode, string message)
    {
        return new CodeActDecision
        {
            Allowed = false,
            ReasonCode = reasonCode,
            Message = message
        };
    }

    public static CodeActDecision RequireApproval(string reasonCode, string message)
    {
        return new CodeActDecision
        {
            Allowed = false,
            ApprovalRequired = true,
            ReasonCode = reasonCode,
            Message = message
        };
    }
}
