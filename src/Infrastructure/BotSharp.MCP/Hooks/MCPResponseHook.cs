using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Infrastructures.Enums;
using BotSharp.Abstraction.Translation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace BotSharp.MCP.Hooks;

public class MCPResponseHook : ConversationHookBase
{
    private readonly IServiceProvider _services;
    private readonly IConversationStateService _states;
    private const string AIAssistant = BuiltInAgentId.AIAssistant;

    public MCPResponseHook(IServiceProvider services,
        IConversationStateService states)
    {
        _services = services;
        _states = states;
    }
    public override async Task OnResponseGenerated(RoleDialogModel message)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);
        if(agent.McpTools.Any(item => item.Functions.Any(x=> x.Name == message.FunctionName)))
        {
            message.Data = JsonSerializer.Deserialize(message.Content, typeof(object));
        }
        await base.OnResponseGenerated(message);
    }
}
 
