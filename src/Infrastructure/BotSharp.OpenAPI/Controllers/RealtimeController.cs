using BotSharp.Abstraction.Realtime.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Core.Infrastructures;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class RealtimeController : ControllerBase
{
    private readonly IServiceProvider _services;

    public RealtimeController(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// Create an ephemeral API token for use in client-side applications with the Realtime API.
    /// </summary>
    /// <returns></returns>
    [HttpGet("/agent/{agentId}/realtime/session")]
    public async Task<RealtimeSession> CreateSession(string agentId)
    {
        var completion = CompletionProvider.GetRealTimeCompletion(_services, provider: "openai", modelId: "gpt-4");

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        return await completion.CreateSession(agent, []);
    }

    [HttpPost("/agent/{agentId}/function/{functionName}/execute")]
    public async Task<string> ExecuteFunction(string agentId, string functionName, [FromBody] JsonDocument args)
    {
        // var agentService = _services.GetRequiredService<IAgentService>();
        // var agent = await agentService.LoadAgent(agentId);
        var routing = _services.GetRequiredService<IRoutingService>();
        // Call functions
        var message = new RoleDialogModel(AgentRole.Function, "")
        {
            FunctionName = functionName,
            FunctionArgs = JsonSerializer.Serialize(args)
        };
        await routing.InvokeFunction(functionName, message);
        return message.Content;
    }
}
