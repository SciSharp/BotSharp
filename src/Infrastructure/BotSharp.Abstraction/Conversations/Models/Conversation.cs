using BotSharp.Abstraction.Conversations.Enums;

namespace BotSharp.Abstraction.Conversations.Models;

public class Conversation
{
    public string Id { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    [JsonIgnore]
    public string Dialog { get; set; } = string.Empty;

    [JsonIgnore]
    public ConversationState States { get; set; }

    public string Status { get; set; } = ConversationStatus.Open;

    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
