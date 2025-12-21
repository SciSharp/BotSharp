using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Plugin.A2A.Services;
using BotSharp.Plugin.A2A.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BotSharp.Plugin.A2A.Functions;

public class A2ADelegationFn : IFunctionCallback
{
    public string Name => "delegate_to_a2a";
    public string Indication => "Connecting to external agent network..."; 

    private readonly IA2AService _a2aService;
    private readonly A2ASettings _settings;
    private readonly IConversationStateService _stateService;

    public A2ADelegationFn(IA2AService a2aService, A2ASettings settings, IConversationStateService stateService)
    {
        _a2aService = a2aService;
        _settings = settings;
        _stateService = stateService;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<JsonElement>(message.FunctionArgs);
        string queryText = string.Empty;
        if (args.TryGetProperty("user_query", out var queryProp))
        {
            queryText = queryProp.GetString();
        }

        var agentId = message.CurrentAgentId;
        var agentConfig = _settings.Agents.FirstOrDefault(x => x.Id == agentId);

        if (agentConfig == null)
        {
            message.Content = "System Error: Remote agent configuration not found.";
            message.StopCompletion = true;  
            return false;
        }

        var conversationId = _stateService.GetConversationId();

        try
        {
            var responseText = await _a2aService.SendMessageAsync(
                agentConfig.Endpoint,
                queryText,
                conversationId,
                CancellationToken.None
            );

            message.Content = responseText; 
            return true;
        }
        catch (Exception ex)
        {            
            message.Content = $"Communication failure with external agent: {ex.Message}";
            return false;
        }
    }
}
