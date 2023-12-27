using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class ConversationViewModel
{
    public string Id { get; set; }

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public UserViewModel User {  get; set; } = new UserViewModel();

    [JsonPropertyName("unread_msg_count")]
    public int UnreadMsgCount { get; set; }

    public string Event { get; set; }

    public string Channel { get; set; } = ConversationChannel.OpenAPI;
    public string Status { get; set; }
    public ConversationState States { get; set; }

    [JsonPropertyName("updated_time")]
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public static ConversationViewModel FromSession(Conversation sess)
    {
        return new ConversationViewModel
        {
            Id = sess.Id,
            User = new UserViewModel 
            { 
                Id = sess.UserId 
            },
            AgentId = sess.AgentId,
            Title = sess.Title,
            Channel = sess.Channel,
            Status = sess.Status,
            CreatedTime = sess.CreatedTime,
            UpdatedTime = sess.UpdatedTime
        };
    }
}
