using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Hooks;

namespace BotSharp.Core.Routing.Functions;

public class HumanInterventionNeededFn : IFunctionCallback
{
    public string Name => "human_intervention_needed";

    private readonly IServiceProvider _services;

    public HumanInterventionNeededFn(IServiceProvider services)
    {
        _services = services;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var hooks = _services.GetHooksOrderByPriority<IConversationHook>(message.CurrentAgentId);

        foreach (var hook in hooks)
        {
            await hook.OnHumanInterventionNeeded(message);
        }

        return true;
    }
}
