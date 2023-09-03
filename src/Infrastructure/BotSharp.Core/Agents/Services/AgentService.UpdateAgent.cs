using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task UpdateAgent(Agent agent)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();

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

        if (!agent.Functions.IsEmpty())
            record.Functions = agent.Functions;

        if (!agent.Responses.IsEmpty())
            record.Responses = agent.Responses;

        db.UpdateAgent(record);
        await Task.CompletedTask;
    }
}
