using BotSharp.Abstraction.Conversations.Enums;

namespace BotSharp.Abstraction.Conversations.Models;

public class Conversation
{
    public string Id { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Agent task id
    /// </summary>
    public string? TaskId { get; set; }
    public string Title { get; set; } = string.Empty;

    [JsonIgnore]
    public List<DialogElement> Dialogs { get; set; } = new List<DialogElement>();

    [JsonIgnore]
    public Dictionary<string, string> States { get; set; } = new Dictionary<string, string>();

    public string Status { get; set; } = ConversationStatus.Open;

    public string Channel { get; set; } = ConversationChannel.OpenAPI;

    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}

public class DialogElement
{
    public DialogMeta MetaData { get; set; }
    public string Content { get; set; }

    public DialogElement()
    {

    }

    public DialogElement(DialogMeta meta, string content)
    {
        MetaData = meta;
        Content = content;
    }
}

public class DialogMeta
{
    public string Role { get; set; }
    public string AgentId { get; set; }
    public string MessageId { get; set; }
    public string? FunctionName { get; set; }
    public string? SenderId { get; set; }
    public DateTime CreateTime { get; set; }
}
