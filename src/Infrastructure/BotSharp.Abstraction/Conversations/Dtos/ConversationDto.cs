using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Users.Dtos;

namespace BotSharp.Abstraction.Conversations.Dtos;

public class ConversationDto
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

    public UserDto User { get; set; } = new UserDto();

    public string Channel { get; set; } = ConversationChannel.OpenAPI;

    /// <summary>
    /// Agent task id
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    public string Status { get; set; }
    public Dictionary<string, string> States { get; set; } = [];

    public List<string> Tags { get; set; } = new();

    [JsonPropertyName("updated_time")]
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

    public static ConversationDto FromSession(Conversation sess)
    {
        return new ConversationDto
        {
            Id = sess.Id,
            User = new UserDto
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
