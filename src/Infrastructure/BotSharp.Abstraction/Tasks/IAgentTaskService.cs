using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Tasks.Models;

namespace BotSharp.Abstraction.Tasks;

public interface IAgentTaskService
{
    Task<PagedItems<AgentTask>> GetTasks(AgentTaskFilter filter);
}
