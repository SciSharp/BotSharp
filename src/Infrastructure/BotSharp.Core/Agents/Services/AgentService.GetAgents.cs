using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<List<Agent>> GetAgents()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var query = from a in db.Agent
                    join ua in db.UserAgent on a.Id equals ua.AgentId
                    join u in db.User on ua.UserId equals u.Id
                    where u.ExternalId == _user.Id
                    select a.ToAgent();
        return query.ToList();
    }

    public async Task<Agent> GetAgent(string id)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        //var query = from agent in db.Agent
        //            where agent.Id == id
        //            select agent.ToAgent();

        //var profile = query.FirstOrDefault();
        var profile = db.GetAgent(id)?.ToAgent();
        //var dir = GetAgentDataDir(id);

        var instructionFile = profile?.Instruction;
        if (instructionFile != null)
        {
            profile.Instruction = instructionFile;
        }
        else
        {
            _logger.LogError($"Can't find instruction file from {instructionFile}");
        }

        var samplesFile = profile?.Samples;
        if (samplesFile != null)
        {
            profile.Samples = samplesFile;
        }

        var functionsFile = profile?.Functions;
        if (functionsFile != null)
        {
            profile.Functions = functionsFile;
        }

        return profile;
    }
}
