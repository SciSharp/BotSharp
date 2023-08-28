using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.Repositories.Records;

public class ConversationRecord : RecordBase
{
    [Required]
    [MaxLength(36)]
    public string AgentId { get; set; } = string.Empty;

    [Required]
    [MaxLength(36)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string Title { get; set; } = string.Empty;

    public string Dialog { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;

    [Required]
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public static ConversationRecord FromConversation(Conversation conv)
    {
        return new ConversationRecord
        {
            AgentId = conv.AgentId,
            UserId = conv.UserId,
            Id = conv.Id,
            Title = conv.Title,
            CreatedTime = conv.CreatedTime,
            UpdatedTime = conv.UpdatedTime
        };
    }

    public Conversation ToConversation()
    {
        return new Conversation
        {
            Id = Id,
            Title = Title,
            UserId = UserId,
            AgentId = AgentId,
            CreatedTime = CreatedTime,
            UpdatedTime = UpdatedTime
        };
    }
}
