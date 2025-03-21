using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.MCP.Hooks;

public class MCPResponseHook : ConversationHookBase
{
    private readonly IServiceProvider _services;
    private readonly IConversationStateService _states; 

    public MCPResponseHook(IServiceProvider services,
        IConversationStateService states)
    {
        _services = services;
        _states = states;
    }
    public override async Task OnResponseGenerated(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);
        if(agent.McpTools.Any(item => item.Functions.Any(x=> x.Name == message.FunctionName)))
        {
            var data = JsonDocument.Parse(JsonSerializer.Serialize(message.Data));
            state.SaveStateByArgs(data);
        }
        await base.OnResponseGenerated(message);
    }
}
 
