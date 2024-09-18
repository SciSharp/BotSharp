using BotSharp.Abstraction.Infrastructures;

namespace BotSharp.Abstraction.Browsing.Models;

public class MessageInfo : ICacheKey
{
    public string AgentId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string ContextId { get; set; } = null!;
    public string? MessageId { get; set; }
    public string? TaskId { get; set; }
    public string StepId { get; set; } = Guid.NewGuid().ToString();

    public string GetCacheKey()
        => $"{nameof(MessageInfo)}";
}
