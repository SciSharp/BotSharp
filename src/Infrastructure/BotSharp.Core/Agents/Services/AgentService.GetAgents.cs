using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public async Task<List<Agent>> GetAgents()
    {
        var query = from a in _db.Agents
                    join ua in _db.UserAgents on a.Id equals ua.AgentId
                    join u in _db.Users on ua.UserId equals u.Id
                    where ua.UserId == _user.Id || u.ExternalId == _user.Id || a.IsPublic
                    select a;
        return query.ToList();
    }

#if !DEBUG
    [MemoryCache(10 * 60)]
#endif
    public async Task<Agent> GetAgent(string id)
    {
        var settings = _services.GetRequiredService<RoutingSettings>();
        var routerInstance = _services.GetRequiredService<IRouterInstance>();
        if (settings.RouterId == id)
        {
            return routerInstance.Load().Router;
        }

        var profile = _db.GetAgent(id);

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
