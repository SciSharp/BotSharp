using BotSharp.Abstraction.Agents.Options;
using BotSharp.Abstraction.Coding;
using BotSharp.Abstraction.Coding.Options;

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
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return false;
        }

        codeScripts ??= new();
        var db = _services.GetRequiredService<IBotSharpRepository>();

        if (options?.DeleteIfNotIncluded == true && codeScripts.IsNullOrEmpty())
        {
            // Delete all code scripts in this agent
            db.DeleteAgentCodeScripts(agentId);
            return true;
        }

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

    public async Task<bool> DeleteAgentCodeScripts(string agentId, List<AgentCodeScript>? codeScripts = null)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return false;
        }

        var db = _services.GetRequiredService<IBotSharpRepository>();
        var deleted = db.DeleteAgentCodeScripts(agentId, codeScripts);
        return await Task.FromResult(deleted);
    }

    public async Task<CodeGenerationResult> GenerateCodeScript(string agentId, string text, CodeProcessOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            return new CodeGenerationResult
            {
                ErrorMsg = "Agent id cannot be empty."
            };
        }

        var settings = _services.GetRequiredService<CodingSettings>();
        var processor = options?.Processor ?? settings?.CodeGeneration?.Processor;
        processor = !string.IsNullOrEmpty(processor) ? processor : "botsharp-py-interpreter";
        var codeProcessor = _services.GetServices<ICodeProcessor>().FirstOrDefault(x => x.Provider.IsEqualTo(processor));
        if (codeProcessor == null)
        {
            var errorMsg = $"Unable to find code processor {processor}.";
            _logger.LogWarning(errorMsg);
            return new CodeGenerationResult
            {
                ErrorMsg = errorMsg
            };
        }

        var result = await codeProcessor.GenerateCodeScriptAsync(text, options);
        if (result.Success && options?.SaveToDb == true)
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var scripts = new List<AgentCodeScript>
            {
                new AgentCodeScript
                {
                    Name = options?.ScriptName ?? $"{Guid.NewGuid()}.py",
                    Content = result.Content,
                    ScriptType = options?.ScriptType ?? AgentCodeScriptType.Src
                }
            };
            var saved = db.UpdateAgentCodeScripts(agentId, scripts, new() { IsUpsert = true });
            result.Success = saved;
        }

        return result;
    }
}
