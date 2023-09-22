using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Routing.Settings;

namespace BotSharp.Core.Routing.Handlers;

public class ConversationEndRoutingHandler : RoutingHandlerBase, IRoutingHandler
{
    public string Name => "conversation_end";

    public string Description => "Call this function when user wants to end this conversation or all tasks have been completed.";

    public List<string> Parameters => new List<string>
    {
    };

    public bool IsReasoning => false;

    public ConversationEndRoutingHandler(IServiceProvider services, ILogger<ConversationEndRoutingHandler> logger, RoutingSettings settings) 
        : base(services, logger, settings)
    {
    }

    public Task<RoleDialogModel> Handle(FunctionCallFromLlm inst)
    {
        throw new NotImplementedException();
    }
}
