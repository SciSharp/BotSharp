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
        var logs = await db.GetConversationContentLogs(conversationId, filter);
        return logs;
    }


    public async Task<DateTimePagination<ConversationStateLogModel>> GetConversationStateLogs(string conversationId, ConversationLogFilter filter)
    {
        if (filter == null)
        {
            filter = ConversationLogFilter.Empty();
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var logs = await db.GetConversationStateLogs(conversationId, filter);
        return logs;
    }
}
