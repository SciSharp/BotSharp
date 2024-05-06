using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    private int _currentRecursionDepth = 0;
    public async Task<bool> InvokeAgent(string agentId, List<RoleDialogModel> dialogs, Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        _currentRecursionDepth++;
        if (_currentRecursionDepth > agent.LlmConfig.MaxRecursionDepth)
        {
            _logger.LogWarning($"Current recursive call depth greater than {agent.LlmConfig.MaxRecursionDepth}, which will cause unexpected result.");
            return false;
        }

        var provide = agent.LlmConfig.Provider;
        var model = agent.LlmConfig.Model;

        if (provide == null || model == null)
        {
            var agentSettings = _services.GetRequiredService<AgentSettings>();
            provide = agentSettings.LlmConfig.Provider;
            model = agentSettings.LlmConfig.Model;
        }

        var chatCompletion = CompletionProvider.GetChatCompletion(_services, 
            provider: provide,
            model: model);

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
            message.ToolCallId = response.ToolCallId;
            message.FunctionName = response.FunctionName;
            message.FunctionArgs = response.FunctionArgs;
            message.CurrentAgentId = agent.Id;

            await InvokeFunction(message, dialogs, onFunctionExecuting);
        }
        else
        {
            // Handle output routing exception.
            if (agent.Type == AgentType.Routing)
            {
                response.Content = "Apologies, I'm not quite sure I understand. Could you please provide additional clarification or context?";
            }

            message = RoleDialogModel.From(message,
                    role: AgentRole.Assistant,
                    content: response.Content);
            message.CurrentAgentId = agent.Id;
            dialogs.Add(message);
        }

        return true;
    }

    private async Task<bool> InvokeFunction(RoleDialogModel message, List<RoleDialogModel> dialogs, Func<RoleDialogModel, Task>? onFunctionExecuting = null)
    {
        // execute function
        // Save states
        var states = _services.GetRequiredService<IConversationStateService>();
        states.SaveStateByArgs(message.FunctionArgs?.JsonContent<JsonDocument>());

        var routing = _services.GetRequiredService<IRoutingService>();
        // Call functions
        await routing.InvokeFunction(message.FunctionName, message, onFunctionExecuting);

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
                await InvokeAgent(agentId, dialogs, onFunctionExecuting);
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
