using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Tasks.Models;

namespace BotSharp.Abstraction.Tasks;

public interface IAgentTaskService
{
    Task<PagedItems<AgentTask>> GetTasks(AgentTaskFilter filter);

    Task<AgentTask?> GetTask(string agentId, string taskId);

    Task CreateTask(AgentTask task);

    Task UpdateTask(AgentTask task, AgentTaskField field);

    Task<bool> DeleteTask(string agentId, string taskId);
}
