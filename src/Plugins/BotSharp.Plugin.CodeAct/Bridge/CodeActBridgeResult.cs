namespace BotSharp.Plugin.CodeAct.Bridge;

public class CodeActBridgeResult
{
    public bool Success { get; set; }
    public bool ApprovalRequired { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public object? Data { get; set; }
    public object? RichContent { get; set; }
    public bool StopCompletion { get; set; }
    public string? MessageLabel { get; set; }
    public List<CodeActTrace> Trace { get; set; } = [];

    public static CodeActBridgeResult FromDecision(CodeActDecision decision)
    {
        return new CodeActBridgeResult
        {
            Success = false,
            ApprovalRequired = decision.ApprovalRequired,
            ReasonCode = decision.ReasonCode,
            Content = decision.Message,
            Trace =
            [
                new CodeActTrace
                {
                    Event = decision.ApprovalRequired ? "bridge.approval_required" : "bridge.denied",
                    Component = "CodeActBridge",
                    Message = decision.Message,
                    Attributes = new Dictionary<string, object?> { ["reason_code"] = decision.ReasonCode }
                }
            ]
        };
    }

    public static CodeActBridgeResult FromTokenValidation(CodeActTokenValidationResult validation)
    {
        return new CodeActBridgeResult
        {
            Success = false,
            ReasonCode = validation.ReasonCode,
            Content = validation.Message,
            Trace =
            [
                new CodeActTrace
                {
                    Event = "bridge.token_rejected",
                    Component = "CodeActBridge",
                    Message = validation.Message,
                    Attributes = new Dictionary<string, object?> { ["reason_code"] = validation.ReasonCode }
                }
            ]
        };
    }
}
