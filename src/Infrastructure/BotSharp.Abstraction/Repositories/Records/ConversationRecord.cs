using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Repositories.Models;
using System.Text.Json.Serialization;

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

    [JsonIgnore]
    public string Dialog { get; set; }

    [JsonIgnore]
    public List<KeyValueModel> State { get; set; }

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
            Dialog = conv.Dialog,
            State = conv.State?.Select(x => new KeyValueModel(x.Key, x.Value))?.ToList() ?? new List<KeyValueModel>(),
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
            Dialog = Dialog,
            State = new ConversationState(State),
            CreatedTime = CreatedTime,
            UpdatedTime = UpdatedTime
        };
    }
}
