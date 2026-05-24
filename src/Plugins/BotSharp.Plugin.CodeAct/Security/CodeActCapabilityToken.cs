namespace BotSharp.Plugin.CodeAct.Security;

public class CodeActCapabilityToken
{
    public string Token { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string ConversationId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}
