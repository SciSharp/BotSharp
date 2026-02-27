using BotSharp.Abstraction.Conversations.Dtos;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class ConversationViewModel : ConversationDto
{
    [JsonPropertyName("is_realtime_enabled")]
    public bool IsRealtimeEnabled { get; set; }

    [JsonPropertyName("thumbnail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Thumbnail { get; set; }

    public static ConversationViewModel FromSession(Conversation sess)
    {
        return new ConversationViewModel
        {
            Id = sess.Id,
            User = new() 
            { 
                Id = sess.UserId 
            },
            AgentId = sess.AgentId,
            Title = sess.Title,
            TitleAlias = sess.TitleAlias,
            Channel = sess.Channel,
            Status = sess.Status,
            TaskId = sess.TaskId,
            Tags = sess.Tags ?? [],
            States = sess.States ?? [],
            CreatedTime = sess.CreatedTime,
            UpdatedTime = sess.UpdatedTime
        };
    }
}
