using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class ResponseToUserRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "response_to_user";

    public string Description => "Response according to the context without asking specific agent.";

    public bool IsReasoning => false;

    public List<NameDesc> Parameters => new List<NameDesc>
    {
        new NameDesc("reason", "why response to user"),
        new NameDesc("response", "response content")
    };

    public ResponseToUserRoutingHandler(IServiceProvider services, ILogger<ResponseToUserRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<RoleDialogModel> Handle(IRoutingService routing, FunctionCallFromLlm inst)
    {
        var result = new RoleDialogModel(AgentRole.Assistant, inst.Response)
        {
            CurrentAgentId = _settings.RouterId,
            FunctionName = inst.Function,
            Data = inst,
            StopCompletion = true
        };
        return result;
    }
}
