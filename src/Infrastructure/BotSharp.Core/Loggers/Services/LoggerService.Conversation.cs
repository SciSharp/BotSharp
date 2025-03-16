using BotSharp.Abstraction.Loggers.Models;

namespace BotSharp.Core.Loggers.Services;

public partial class LoggerService
{
    public async Task<DateTimePagination<ContentLogOutputModel>> GetConversationContentLogs(string conversationId, ConversationLogFilter filter)
    {
        if (filter == null)
        {
            filter = ConversationLogFilter.Empty();
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var logs = db.GetConversationContentLogs(conversationId, filter);
        return await Task.FromResult(logs);
    }


    public async Task<DateTimePagination<ConversationStateLogModel>> GetConversationStateLogs(string conversationId, ConversationLogFilter filter)
    {
        if (filter == null)
        {
            filter = ConversationLogFilter.Empty();
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var logs = db.GetConversationStateLogs(conversationId, filter);
        return await Task.FromResult(logs);
    }
}
