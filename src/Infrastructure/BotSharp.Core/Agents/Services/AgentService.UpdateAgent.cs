using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task UpdateAgent(Agent agent)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

        db.Transaction<IBotSharpTable>(delegate
        {
            var record = (from a in db.Agent
                          join ua in db.UserAgent on a.Id equals ua.AgentId
                          join u in db.User on ua.UserId equals u.Id
                          where (ua.UserId == _user.Id || u.ExternalId == _user.Id) && 
                            a.Id == agent.Id
                          select a).FirstOrDefault();

            if (record == null) return;

            record.Name = agent.Name;

            if (!string.IsNullOrEmpty(agent.Description))
                record.Description = agent.Description;

            if (!string.IsNullOrEmpty(agent.Instruction))
                record.Instruction = agent.Instruction;

            if (!string.IsNullOrEmpty(agent.Functions))
                record.Functions = agent.Functions;

            if (!agent.Routes.IsEmpty())
                record.Routes = agent.Routes;

            record.UpdatedTime = DateTime.UtcNow;
            db.Add<IBotSharpTable>(record);
        });

        // Save instruction to file
        //var dir = GetAgentDataDir(agent.Id);
        //var instructionFile = Path.Combine(dir, "instruction.txt");
        //File.WriteAllText(instructionFile, agent.Instruction);

        //var samplesFile = Path.Combine(dir, "samples.txt");
        //File.WriteAllText(samplesFile, agent.Samples);
    }
}
