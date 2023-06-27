using BotSharp.Abstraction.Agents.Models;
using System.IO;

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

    public async Task<Agent> GetAgent(string id)
    {
        var db = _services.GetRequiredService<AgentDbContext>();
        var query = from agent in db.Agent
                    where agent.OwnerId == _user.Id && agent.Id == id
                    select agent.ToAgent();

        var profile = query.FirstOrDefault();
        var dir = GetAgentDataDir(id);

        profile.Instruction = File.ReadAllText(Path.Combine(dir, "instruction.txt"));
        profile.Samples = File.ReadAllText(Path.Combine(dir, "samples.txt"));

        return profile;
    }
}
