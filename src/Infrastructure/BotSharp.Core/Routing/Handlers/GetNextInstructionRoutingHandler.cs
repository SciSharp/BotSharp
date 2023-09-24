using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class GetNextInstructionRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "get_next_instruction";

    public string Description => "";

    public bool IsReasoning => false;

    public GetNextInstructionRoutingHandler(IServiceProvider services, ILogger<GetNextInstructionRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<RoleDialogModel> Handle(FunctionCallFromLlm inst)
    {
        throw new NotImplementedException();
    }
}
