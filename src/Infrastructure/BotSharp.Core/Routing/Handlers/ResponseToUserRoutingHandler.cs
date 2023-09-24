using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class ResponseToUserRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "response_to_user";

    public string Description => "You know how to response according to the context, don't need to ask specific agent.";

    public List<NameDesc> Parameters => new List<NameDesc>
    {
        new NameDesc("answer", "the content of response"),
        new NameDesc("reason", "why response to user")
    };

    public bool IsReasoning => false;

    public ResponseToUserRoutingHandler(IServiceProvider services, ILogger<ResponseToUserRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<RoleDialogModel> Handle(FunctionCallFromLlm inst)
    {
        var result = new RoleDialogModel(AgentRole.Assistant, inst.Answer)
        {
            CurrentAgentId = _settings.RouterId,
            FunctionName = inst.Function,
            StopCompletion = true
        };
        return result;
    }
}
