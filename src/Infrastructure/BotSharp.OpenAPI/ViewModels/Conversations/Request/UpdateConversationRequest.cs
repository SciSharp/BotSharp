namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class UpdateConversationRequest
{
    public List<string> ToAddTags { get; set; } = [];
    public List<string> ToDeleteTags { get; set; } = [];
}
