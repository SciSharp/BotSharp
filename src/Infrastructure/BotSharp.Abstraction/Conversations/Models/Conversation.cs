using BotSharp.Abstraction.Conversations.Enums;

namespace BotSharp.Abstraction.Conversations.Models;

public class Conversation
{
    public string Id { get; set; } = string.Empty;
    public string AgentId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    [JsonIgnore]
    public List<DialogElement> Dialogs { get; set; } = new List<DialogElement>();

    [JsonIgnore]
    public ConversationState States { get; set; } = new ConversationState();

    public string Status { get; set; } = ConversationStatus.Open;

    public string Channel { get; set; } = ConversationChannel.OpenAPI;

    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}

public class DialogElement
{
    public string MetaData { get; set; }
    public string Content { get; set; }

    public DialogElement()
    {

    }

    public DialogElement(string meta, string content)
    {
        MetaData = meta;
        Content = content;
    }
}
