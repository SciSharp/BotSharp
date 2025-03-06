using BotSharp.Abstraction.Loggers.Models;

namespace BotSharp.Core.Loggers.Services;

public partial class LoggerService
{
    public async Task<List<ContentLogOutputModel>> GetConversationContentLogs(string conversationId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var logs = db.GetConversationContentLogs(conversationId);
        return await Task.FromResult(logs);
    }


    public async Task<List<ConversationStateLogModel>> GetConversationStateLogs(string conversationId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var logs = db.GetConversationStateLogs(conversationId);
        return await Task.FromResult(logs);
    }
}
