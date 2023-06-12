using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Users;

namespace BotSharp.Core.Agents.Services;

public class AgentService : IAgentService
{
    private readonly IServiceProvider _services;
    private readonly ICurrentUser _user;

    public AgentService(IServiceProvider services, ICurrentUser user)
    {
        _services = services;
        _user = user;
    }

    public async Task<Agent> CreateAgent(Agent agent)
    {
        var db = _services.GetRequiredService<AgentDbContext>();
        var record = db.Agent.FirstOrDefault(x => x.OwnerId == _user.Id && x.Name == agent.Name);
        if (record != null)
        {
            return record.ToAgent();
        }

        record = AgentRecord.FromAgent(agent);
        record.Id = Guid.NewGuid().ToString();
        record.OwnerId = _user.Id;
        record.CreatedDateTime = DateTime.UtcNow;
        record.UpdatedDateTime = DateTime.UtcNow;

        db.Transaction<IAgentTable>(delegate
        {
            db.Add<IAgentTable>(record);
        });

        return record.ToAgent();
    }

    public Task<bool> DeleteAgent(string id)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAgent(Agent agent)
    {
        throw new NotImplementedException();
    }
}
