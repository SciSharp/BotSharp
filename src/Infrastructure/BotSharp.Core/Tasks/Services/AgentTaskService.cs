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
        var agentService = _services.GetRequiredService<IAgentService>();
        var agents = await agentService.GetAgents(new AgentFilter());
        var tasks = new List<AgentTask>();
        foreach (var agent in agents.Items)
        {
            if (filter.AgentId != null && filter.AgentId != agent.Id)
            {
                continue;
            }
            agent.Tasks.ForEach(x => x.Agent = agent);
            tasks.AddRange(agent.Tasks);
        }

        return new PagedItems<AgentTask>
        {
            Items = tasks.Skip(filter.Pager.Offset).Take(filter.Pager.Size),
            Count = tasks.Count,
        };
    }
}
