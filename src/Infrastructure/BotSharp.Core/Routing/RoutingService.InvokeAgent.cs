using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    private int _currentRecursionDepth = 0;
    public async Task<bool> InvokeAgent(string agentId, List<RoleDialogModel> dialogs)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        _currentRecursionDepth++;
        if (_currentRecursionDepth > agent.LlmConfig.MaxRecursionDepth)
        {
            _logger.LogWarning($"Current recursive call depth greater than {agent.LlmConfig.MaxRecursionDepth}, which will cause unexpected result.");
            return false;
        }

        var chatCompletion = CompletionProvider.GetChatCompletion(_services, 
            agentConfig: agent.LlmConfig);

        var message = dialogs.Last();
        var response = await chatCompletion.GetChatCompletions(agent, dialogs);

        if (response.Role == AgentRole.Function)
        {
            message = RoleDialogModel.From(message,
                    role: AgentRole.Function);
            if (response.FunctionName != null && response.FunctionName.Contains("/"))
            {
                response.FunctionName = response.FunctionName.Split("/").Last();
            }
            message.FunctionName = response.FunctionName;
            message.FunctionArgs = response.FunctionArgs;
            message.CurrentAgentId = agent.Id;
            await InvokeFunction(message, dialogs);
        }
        else
        {
            message = RoleDialogModel.From(message,
                    role: AgentRole.Assistant,
                    content: response.Content);
            message.CurrentAgentId = agent.Id;
            dialogs.Add(message);
        }

        return true;
    }

    private async Task<bool> InvokeFunction(RoleDialogModel message, List<RoleDialogModel> dialogs)
    {
        // execute function
        // Save states
        var states = _services.GetRequiredService<IConversationStateService>();
        states.SaveStateByArgs(message.FunctionArgs?.JsonContent<JsonDocument>());

        var routing = _services.GetRequiredService<IRoutingService>();
        // Call functions
        await routing.InvokeFunction(message.FunctionName, message);

        // Pass execution result to LLM to get response
        if (!message.StopCompletion)
        {
            // Find response template
            var templateService = _services.GetRequiredService<IResponseTemplateService>();
            var responseTemplate = await templateService.RenderFunctionResponse(message.CurrentAgentId, message);
            if (!string.IsNullOrEmpty(responseTemplate))
            {
                dialogs.Add(RoleDialogModel.From(message,
                    role: AgentRole.Assistant,
                    content: responseTemplate));
            }
            else
            {
                // Save to memory dialogs
                var msg = RoleDialogModel.From(message,
                    role: AgentRole.Function,
                    content: message.Content);

                dialogs.Add(msg);

                // Send to Next LLM
                var agentId = routing.Context.GetCurrentAgentId();
                await InvokeAgent(agentId, dialogs);
            }
        }
        else
        {
            dialogs.Add(RoleDialogModel.From(message,
                role: AgentRole.Assistant,
                content: message.Content));
        }

        return true;
    }
}
