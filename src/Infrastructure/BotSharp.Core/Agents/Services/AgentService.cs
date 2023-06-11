using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Core.Repository;
using BotSharp.Core.Repository.Abstraction;
using BotSharp.Core.Repository.DbTables;
using EntityFrameworkCore.BootKit;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Core.Agents.Services;

public class AgentService : IAgentService
{
    private readonly IServiceProvider _services;
    public AgentService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<string> CreateAgent(Agent agent)
    {
        var db = _services.GetRequiredService<AgentDbContext>();
        var record = AgentRecord.FromAgent(agent);

        db.Transaction<IAgentTable>(delegate
        {
            db.Add<IAgentTable>(record);
        });

        return record.Id;
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
