using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using System.Linq;
using System.Text.Json;

namespace BotSharp.Plugin.PizzaBot.Hooks;

public class PizzaBotConversationHook : ConversationHookBase
{
    private readonly IServiceProvider _services;
    private readonly IConversationStateService _states;

    public PizzaBotConversationHook(IServiceProvider services,
        IConversationStateService states)
    {
        _services = services;
        _states = states;
    }

    public override async Task OnPostbackMessageReceived(RoleDialogModel message, PostbackMessageModel replyMsg)
    {
        if (replyMsg.FunctionName == "get_pizza_types")
        {
            // message.StopCompletion = true;
        }
        return;
    }

    public override Task OnTaskCompleted(RoleDialogModel message)
    {
        return base.OnTaskCompleted(message);
    }

    #if USE_BOTSHARP
    public override async Task OnResponseGenerated(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);

        if (agent.McpTools.Any(item => item.Functions.Any(x => x.Name == message.FunctionName)))
        {
            var data = JsonDocument.Parse(JsonSerializer.Serialize(message.Data));
            state.SaveStateByArgs(data);
        }

        await base.OnResponseGenerated(message);
    }
    #endif
}
