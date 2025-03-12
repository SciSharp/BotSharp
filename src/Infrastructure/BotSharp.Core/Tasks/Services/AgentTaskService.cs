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
            var pagedTasks = db.GetAgentTasks(filter);
            return await Task.FromResult(pagedTasks);
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
        var task = db.GetAgentTask(agentId, taskId);
        return await Task.FromResult(task);
    }

    /// <summary>
    /// Create a new agent task
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public async Task CreateTask(AgentTask task)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.InsertAgentTask(task);
        await Task.CompletedTask;
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
        db.UpdateAgentTask(task, field);
        await Task.CompletedTask;
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
        var isDeleted = db.DeleteAgentTask(agentId, new List<string> { taskId });
        return await Task.FromResult(isDeleted);
    }
}
