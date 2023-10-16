using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class ConversationEndRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "conversation_end";

    public string Description => "User completed his task and wants to end the conversation.";

    public bool IsReasoning => false;

    public ConversationEndRoutingHandler(IServiceProvider services, ILogger<ConversationEndRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public async Task<RoleDialogModel> Handle(IRoutingService routing, FunctionCallFromLlm inst)
    {
        var result = new RoleDialogModel(AgentRole.Assistant, inst.Response)
        {
            CurrentAgentId = _settings.RouterId,
            FunctionName = inst.Function,
            ExecutionData = inst
        };

        var hooks = _services.GetServices<IConversationHook>()
            .OrderBy(x => x.Priority)
            .ToList();

        foreach (var hook in hooks)
        {
            await hook.OnFunctionExecuting(result);
        }

        return result;
    }
}
