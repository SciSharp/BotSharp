using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class ConversationViewModel
{
    public string Id { get; set; }

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; }

    [JsonPropertyName("agent_name")]
    public string AgentName { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("title_alias")]
    public string TitleAlias { get; set; } = string.Empty;

    public UserViewModel User {  get; set; } = new UserViewModel();

    public string Event { get; set; }

    public string Channel { get; set; } = ConversationChannel.OpenAPI;

    /// <summary>
    /// Agent task id
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    public string Status { get; set; }
    public Dictionary<string, string> States { get; set; }

    public List<string> Tags { get; set; } = new();

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
            TitleAlias = sess.TitleAlias,
            Channel = sess.Channel,
            Status = sess.Status,
            TaskId = sess.TaskId,
            Tags = sess.Tags ?? new(),
            CreatedTime = sess.CreatedTime,
            UpdatedTime = sess.UpdatedTime
        };
    }
}
