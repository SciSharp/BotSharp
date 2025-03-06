using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Loggers.Models;

namespace BotSharp.Abstraction.Loggers.Services;

public interface ILoggerService
{
    #region Conversation
    Task<List<ContentLogOutputModel>> GetConversationContentLogs(string conversationId);
    Task<List<ConversationStateLogModel>> GetConversationStateLogs(string conversationId);
    #endregion

    #region Instruction
    Task<PagedItems<InstructionLogModel>> GetInstructionLogs(InstructLogFilter filter);
    #endregion
}
