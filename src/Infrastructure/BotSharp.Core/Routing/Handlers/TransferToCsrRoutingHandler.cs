using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class TransferToCsrRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "transfer_to_csr";

    public string Description => "Reach out to a real customer representative to help.";

    public List<string> Parameters => new List<string>
    {
    };

    public bool IsReasoning => false;

    public TransferToCsrRoutingHandler(IServiceProvider services, ILogger<TransferToCsrRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<RoleDialogModel> Handle(FunctionCallFromLlm inst)
    {
        var result = new RoleDialogModel(AgentRole.User, "I'm transferring to a customer representative, waiting a moment please.")
        {
            CurrentAgentId = _settings.RouterId,
            FunctionName = inst.Function,
            StopCompletion = true
        };
        return result;
    }
}
