using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Repositories.Models;

namespace BotSharp.Abstraction.Loggers.Services;

public interface ILoggerService
{
    #region Conversation
    Task<DateTimePagination<ContentLogOutputModel>> GetConversationContentLogs(string conversationId, ConversationLogFilter filter);
    Task<DateTimePagination<ConversationStateLogModel>> GetConversationStateLogs(string conversationId, ConversationLogFilter filter);
    #endregion

    #region Instruction
    Task<PagedItems<InstructionLogModel>> GetInstructionLogs(InstructLogFilter filter);
    Task<List<string>> GetInstructionLogSearchKeys(InstructLogKeysFilter filter);
    Task<bool> UpdateInstructionLogStates(UpdateInstructionLogStatesModel request);
    #endregion
}
