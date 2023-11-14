using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Repositories;
using System.IO;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task RefreshAgents()
    {
        var isDeleted = _db.DeleteAgents();
        if (!isDeleted) return;

        var dbSettings = _services.GetRequiredService<BotSharpDatabaseSettings>();
        var agentDir = Path.Combine(dbSettings.FileRepository, _agentSettings.DataDir);
        var user = _db.GetUserById(_user.Id);
        var agents = new List<Agent>();
        var userAgents = new List<UserAgent>();

        foreach (var dir in Directory.GetDirectories(agentDir))
        {
            var agentJson = File.ReadAllText(Path.Combine(dir, "agent.json"));
            var agent = JsonSerializer.Deserialize<Agent>(agentJson, _options);
            if (agent == null) continue;

            var functions = FetchFunctionsFromFile(dir);
            var instruction = FetchInstructionFromFile(dir);
            var responses = FetchResponsesFromFile(dir);
            var templates = FetchTemplatesFromFile(dir);
            var samples = FetchSamplesFromFile(dir);
            agent.SetInstruction(instruction)
                 .SetTemplates(templates)
                 .SetFunctions(functions)
                 .SetResponses(responses)
                 .SetSamples(samples);

            var userAgent = new UserAgent
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                AgentId = agent.Id,
                CreatedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow
            };

            agents.Add(agent);
            userAgents.Add(userAgent);
        }

        _db.BulkInsertAgents(agents);
        _db.BulkInsertUserAgents(userAgents);
    }
}
