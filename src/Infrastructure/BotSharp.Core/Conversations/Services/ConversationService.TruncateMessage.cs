namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService : IConversationService
{
    public async Task<bool> TruncateConversation(string conversationId, string messageId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var isSaved = db.TruncateConversation(conversationId, messageId, true);
        var hooks = _services.GetServices<IConversationHook>().ToList();
        foreach (var hook in hooks)
        {
            await hook.OnMessageDeleted(conversationId, messageId);
        }
        return await Task.FromResult(isSaved);
    }
}
