namespace BotSharp.Abstraction.Loggers.Models;

public class ContentLogInputModel
{
    public string ConversationId { get; set; }
    public string? Name { get; set; }
    public string? AgentId { get; set; }
    public string Log { get; set; }
    public string Source { get; set; }
    public RoleDialogModel Message { get; set; }

    public ContentLogInputModel()
    {

    }

    public ContentLogInputModel(string conversationId, RoleDialogModel message)
    {
        ConversationId = conversationId;
        Message = message;
    }
}
