using BotSharp.Abstraction.Agents.Options;

namespace BotSharp.Core.Agents.Services;

public partial class AgentService
{
    public async Task<List<AgentCodeScript>> GetAgentCodeScripts(string agentId, AgentCodeScriptFilter? filter = null)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var scripts = db.GetAgentCodeScripts(agentId, filter);
        return await Task.FromResult(scripts);
    }

    public async Task<string?> GetAgentCodeScript(string agentId, string scriptName, string scriptType = AgentCodeScriptType.Src)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var script = db.GetAgentCodeScript(agentId, scriptName, scriptType);
        return await Task.FromResult(script);
    }

    public async Task<bool> UpdateAgentCodeScripts(string agentId, List<AgentCodeScript> codeScripts, AgentCodeScriptUpdateOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(agentId) || codeScripts == null)
        {
            return false;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();

        var toDeleteScripts = new List<AgentCodeScript>();
        if (options?.DeleteIfNotIncluded == true)
        {
            var curDbScripts = await GetAgentCodeScripts(agentId);
            var codePaths = codeScripts.Select(x => x.CodePath).ToList();
            toDeleteScripts = curDbScripts.Where(x => !codePaths.Contains(x.CodePath)).ToList();
        }

        var updateResult = db.UpdateAgentCodeScripts(agentId, codeScripts, options);
        if (!toDeleteScripts.IsNullOrEmpty())
        {
            db.DeleteAgentCodeScripts(agentId, toDeleteScripts);
        }

        return updateResult;
    }
}
