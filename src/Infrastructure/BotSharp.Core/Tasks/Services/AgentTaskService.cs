using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Repositories.Filters;
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

    public async Task<PagedItems<AgentTask>> GetTasks(AgentTaskFilter filter)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var pagedTasks = db.GetAgentTasks(filter);
        return await Task.FromResult(pagedTasks);
    }

    public async Task<AgentTask?> GetTask(string agentId, string taskId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var task = db.GetAgentTask(agentId, taskId);
        return await Task.FromResult(task);
    }

    public async Task CreateTask(AgentTask task)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.InsertAgentTask(task);
        await Task.CompletedTask;
    }

    public async Task UpdateTask(AgentTask task, AgentTaskField field)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.UpdateAgentTask(task, field);
        await Task.CompletedTask;
    }

    public async Task<bool> DeleteTask(string agentId, string taskId)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var isDeleted = db.DeleteAgentTask(agentId, taskId);
        return await Task.FromResult(isDeleted);
    }
}
