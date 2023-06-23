using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
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
}
