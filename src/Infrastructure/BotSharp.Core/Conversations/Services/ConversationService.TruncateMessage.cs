namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService : IConversationService
{
    public async Task<bool> TruncateConversation(string conversationId, string messageId, string? newMessageId = null)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var fileService = _services.GetRequiredService<IFileBasicService>();
        var deleteMessageIds = db.TruncateConversation(conversationId, messageId, cleanLog: true);

        fileService.DeleteMessageFiles(conversationId, deleteMessageIds, messageId, newMessageId);

        var hooks = _services.GetServices<IConversationHook>().ToList();
        foreach (var hook in hooks)
        {
            await hook.OnMessageDeleted(conversationId, messageId);
        }
        return await Task.FromResult(true);
    }
}
