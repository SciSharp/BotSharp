using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Loggers.Models;

namespace BotSharp.Abstraction.Loggers.Services;

public interface ILoggerService
{
    Task<PagedItems<InstructionLogModel>> GetInstructionLogs(InstructLogFilter filter);
}
