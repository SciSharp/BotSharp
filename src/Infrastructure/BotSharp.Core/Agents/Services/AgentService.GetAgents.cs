using BotSharp.Abstraction.Agents.Models;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<List<Agent>> GetAgents()
    {
        var db = _services.GetRequiredService<BotSharpDbContext>();
        var query = from a in db.Agent
                    join ua in db.UserAgent on a.Id equals ua.AgentId
                    where ua.UserId == _user.Id
                    select a.ToAgent();
        return query.ToList();
    }

    public async Task<Agent> GetAgent(string id)
    {
        var db = _services.GetRequiredService<BotSharpDbContext>();
        var query = from agent in db.Agent
                    where agent.Id == id
                    select agent.ToAgent();

        var profile = query.FirstOrDefault();
        var dir = GetAgentDataDir(id);

        var instructionFile = Path.Combine(dir, "instruction.txt");
        if (File.Exists(instructionFile))
        {
            profile.Instruction = File.ReadAllText(instructionFile);
        }

        var samplesFile = Path.Combine(dir, "samples.txt");
        if (File.Exists(samplesFile))
        {
            profile.Samples = File.ReadAllText(Path.Combine(dir, "samples.txt"));
        }

        return profile;
    }
}
