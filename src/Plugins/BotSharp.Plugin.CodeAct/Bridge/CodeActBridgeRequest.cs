namespace BotSharp.Plugin.CodeAct.Bridge;

public class CodeActBridgeRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string FunctionName { get; set; } = string.Empty;
    public string FunctionArgs { get; set; } = "{}";
    public string CapabilityToken { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Impact { get; set; } = CodeActImpact.Read;
    public Dictionary<string, object?> Metadata { get; set; } = [];
}
