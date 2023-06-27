using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task UpdateAgent(Agent agent)
    {
        var db = _services.GetRequiredService<AgentDbContext>();

        db.Transaction<IAgentTable>(delegate
        {
            var record = db.Agent.FirstOrDefault(x => x.OwnerId == agent.OwerId && x.Id == agent.Id);

            record.Name = agent.Name;
            record.Description = agent.Description;
            record.UpdatedDateTime = DateTime.UtcNow;
        });
    }
}
