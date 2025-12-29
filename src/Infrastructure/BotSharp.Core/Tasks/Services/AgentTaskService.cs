using BotSharp.Abstraction.Tasks;
using BotSharp.Abstraction.Tasks.Models;

namespace BotSharp.Core.Tasks.Services;

public class AgentTaskService : IAgentTaskService
{
    private readonly IServiceProvider _services;
    public AgentTaskService(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// Get agent tasks using pagination
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public async Task<PagedItems<AgentTask>> GetTasks(AgentTaskFilter filter)
    {
        if (filter.Status == TaskStatus.Scheduled)
        {
            var taskFeeders = _services.GetServices<ITaskFeeder>();
            var items = new List<AgentTask>();

            foreach (var feeder in taskFeeders)
            {
                var tasks = await feeder.GetTasks();
                items.AddRange(tasks);
            }

            return new PagedItems<AgentTask>
            {
                Items = items.OrderByDescending(x => x.UpdatedTime)
                            .Skip(filter.Pager.Offset).Take(filter.Pager.Size),
                Count = items.Count()
            };
        }
        else
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var pagedTasks = await db.GetAgentTasks(filter);
            return pagedTasks;
        }
    }

    /// <summary>
    /// Get an agent task
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public async Task<AgentTask?> GetTask(string agentId, string taskId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var task = await db.GetAgentTask(agentId, taskId);
        return task;
    }

    /// <summary>
    /// Create a new agent task
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public async Task CreateTask(AgentTask task)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        await db.InsertAgentTask(task);
    }

    /// <summary>
    /// Update an agent task by a single field or all fields
    /// </summary>
    /// <param name="task"></param>
    /// <param name="field"></param>
    /// <returns></returns>
    public async Task UpdateTask(AgentTask task, AgentTaskField field)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        await db.UpdateAgentTask(task, field);
    }

    /// <summary>
    /// Delete an agent task
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="taskId"></param>
    /// <returns></returns>
    public async Task<bool> DeleteTask(string agentId, string taskId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var isDeleted = await db.DeleteAgentTasks(agentId, new List<string> { taskId });
        return isDeleted;
    }
}
