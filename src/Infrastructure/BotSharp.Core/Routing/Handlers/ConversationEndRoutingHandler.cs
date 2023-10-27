using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class ConversationEndRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "conversation_end";

    public string Description => "User completed his task and wants to end the conversation.";

    public bool IsReasoning => false;

    public List<NameDesc> Parameters => new List<NameDesc>
    {
        new NameDesc("reason", "why end conversation"),
        new NameDesc("response", "response content to user")
    };

    public ConversationEndRoutingHandler(IServiceProvider services, ILogger<ConversationEndRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }


    public async Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message)
    {
        message.Content = inst.Response;
        message.FunctionName = inst.Function;

        var hooks = _services.GetServices<IConversationHook>()
            .OrderBy(x => x.Priority)
            .ToList();

        foreach (var hook in hooks)
        {
            await hook.OnConversationEnding(message);
        }

        return true;
    }
}
