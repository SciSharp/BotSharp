namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService : IConversationService
{
    public async Task<bool> TruncateConversation(string conversationId, string messageId, string? newMessageId = null)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var deleteMessageIds = db.TruncateConversation(conversationId, messageId, cleanLog: true);
        fileStorage.DeleteMessageFiles(conversationId, deleteMessageIds, messageId, newMessageId);

        var hooks = _services.GetServices<IConversationHook>();
        foreach (var hook in hooks)
        {
            await hook.OnMessageDeleted(conversationId, messageId);
        }
        return await Task.FromResult(true);
    }
}
