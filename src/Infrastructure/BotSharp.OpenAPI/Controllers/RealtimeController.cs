using BotSharp.Abstraction.Routing;

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
