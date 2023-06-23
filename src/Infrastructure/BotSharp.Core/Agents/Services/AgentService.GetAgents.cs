using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<List<Agent>> GetAgents()
    {
        var db = _services.GetRequiredService<AgentDbContext>();
        var query = from agent in db.Agent
                    where agent.OwnerId == _user.Id
                    select agent.ToAgent();
        return query.ToList();
    }
}
