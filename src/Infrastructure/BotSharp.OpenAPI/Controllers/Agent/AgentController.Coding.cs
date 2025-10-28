using BotSharp.Abstraction.Coding.Models;
using BotSharp.Abstraction.Infrastructures.Attributes;

namespace BotSharp.OpenAPI.Controllers;

public partial class AgentController
{
    /// <summary>
    /// Get agent code scripts
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("/agent/{agentId}/code-scripts")]
    public async Task<List<AgentCodeScriptViewModel>> GetAgentCodeScripts([FromRoute] string agentId, [FromQuery] AgentCodeScriptFilter request)
    {
        var scripts = await _agentService.GetAgentCodeScripts(agentId, request);
        return scripts.Select(x => AgentCodeScriptViewModel.From(x)).ToList();
    }

    /// <summary>
    /// Update agent code scripts
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [BotSharpAuth]
    [HttpPost("/agent/{agentId}/code-scripts")]
    public async Task<bool> UpdateAgentCodeScripts([FromRoute] string agentId, [FromBody] AgentCodeScriptUpdateModel request)
    {
        var scripts = request?.CodeScripts?.Select(x => AgentCodeScriptViewModel.To(x))?.ToList() ?? [];
        var updated = await _agentService.UpdateAgentCodeScripts(agentId, scripts, request?.Options);
        return updated;
    }

    /// <summary>
    /// Delete agent code scripts
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="request"></param>
    /// <returns></returns>
    [BotSharpAuth]
    [HttpDelete("/agent/{agentId}/code-scripts")]
    public async Task<bool> DeleteAgentCodeScripts([FromRoute] string agentId, [FromBody] AgentCodeScriptDeleteModel request)
    {
        var scripts = request?.CodeScripts?.Select(x => AgentCodeScriptViewModel.To(x))?.ToList();
        var updated = await _agentService.DeleteAgentCodeScripts(agentId, scripts);
        return updated;
    }

    [HttpPost("/agent/{agentId}/code-script/generate")]
    public async Task<CodeGenerationResult> GenerateAgentCodeScript([FromRoute] string agentId, [FromBody] AgentCodeScriptGenerationRequest request)
    {
        request ??= new();
        var states = request.Options?.Data?.ToList();
        var state = _services.GetRequiredService<IConversationStateService>();
        states?.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        state.SetState("programming_language", request.Options?.Language, source: StateSource.External);

        var result = await _agentService.GenerateCodeScript(agentId, request.Text, request?.Options);
        return result;
    }
}
