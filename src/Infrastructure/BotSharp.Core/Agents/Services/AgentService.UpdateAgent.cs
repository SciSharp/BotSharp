using BotSharp.Abstraction.Agents.Models;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task UpdateAgent(Agent agent)
    {
        var db = _services.GetRequiredService<BotSharpDbContext>();

        db.Transaction<IBotSharpTable>(delegate
        {
            var record = (from a in db.Agent
                          join ua in db.UserAgent on a.Id equals ua.AgentId
                          where ua.UserId == agent.OwerId && a.Id == agent.Id
                          select a).First();

            record.Name = agent.Name;
            record.Description = agent.Description;
            record.UpdatedDateTime = DateTime.UtcNow;
        });

        // Save instruction to file
        var dir = GetAgentDataDir(agent.Id);
        var instructionFile = Path.Combine(dir, "instruction.txt");
        File.WriteAllText(instructionFile, agent.Instruction);

        var samplesFile = Path.Combine(dir, "samples.txt");
        File.WriteAllText(samplesFile, agent.Samples);
    }
}
