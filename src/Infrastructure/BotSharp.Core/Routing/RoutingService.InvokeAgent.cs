using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<bool> InvokeAgent(
        string agentId,
        List<RoleDialogModel> dialogs,
        InvokeAgentOptions? options = null)
    {
        options ??= InvokeAgentOptions.Default();
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        Context.IncreaseRecursiveCounter();
        if (Context.CurrentRecursionDepth > agent.LlmConfig.MaxRecursionDepth)
        {
            _logger.LogWarning($"Current recursive call depth greater than {agent.LlmConfig.MaxRecursionDepth}, which will cause unexpected result.");
            return false;
        }

        var provider = agent.LlmConfig.Provider;
        var model = agent.LlmConfig.Model;

        if (provider == null || model == null)
        {
            var agentSettings = _services.GetRequiredService<AgentSettings>();
            provider = agentSettings.LlmConfig.Provider;
            model = agentSettings.LlmConfig.Model;
        }

        var chatCompletion = CompletionProvider.GetChatCompletion(_services, 
            provider: provider,
            model: model);

        RoleDialogModel response;
        var message = dialogs.Last();
        if (options?.UseStream == true)
        {
            response = await chatCompletion.GetChatCompletionsStreamingAsync(agent, dialogs);
        }
        else
        {
            response = await chatCompletion.GetChatCompletions(agent, dialogs);
        }

        if (response.Role == AgentRole.Function)
        {
            message = RoleDialogModel.From(message, role: AgentRole.Function);
            if (response.FunctionName != null && response.FunctionName.Contains("/"))
            {
                response.FunctionName = response.FunctionName.Split("/").Last();
            }
            message.ToolCallId = response.ToolCallId;
            message.FunctionName = response.FunctionName;
            message.FunctionArgs = response.FunctionArgs;
            message.Indication = response.Indication;
            message.CurrentAgentId = agent.Id;
            message.IsStreaming = response.IsStreaming;
            message.AdditionalMessageWrapper = null;

            await InvokeFunction(message, dialogs, options);
        }
        else
        {
            // Handle output routing exception.
            if (agent.Type == AgentType.Routing)
            {
                // Forgot about what situation needs to handle in this way
                response.Content = "Apologies, I'm not quite sure I understand. Could you please provide additional clarification or context?";
            }

            message = RoleDialogModel.From(message, role: AgentRole.Assistant, content: response.Content);
            message.CurrentAgentId = agent.Id;
            message.IsStreaming = response.IsStreaming;
            message.AdditionalMessageWrapper = null;
            dialogs.Add(message);
            Context.SetDialogs(dialogs);
        }

        return true;
    }

    private async Task<bool> InvokeFunction(
        RoleDialogModel message,
        List<RoleDialogModel> dialogs,
        InvokeAgentOptions? options = null)
    {
        // execute function
        // Save states
        var states = _services.GetRequiredService<IConversationStateService>();
        states.SaveStateByArgs(message.FunctionArgs?.JsonContent<JsonDocument>());

        var routing = _services.GetRequiredService<IRoutingService>();
        // Call functions
        var funcOptions = options != null ? new InvokeFunctionOptions() { From = options.From } : null;
        await routing.InvokeFunction(message.FunctionName, message, options: funcOptions);

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
                Context.SetDialogs(dialogs);
            }
            else
            {
                // Save to memory dialogs
                var msg = RoleDialogModel.From(message,
                    role: AgentRole.Function,
                    content: message.Content);

                dialogs.Add(msg);
                Context.SetDialogs(dialogs);

                // Send to Next LLM
                var curAgentId = routing.Context.GetCurrentAgentId();
                await InvokeAgent(curAgentId, dialogs, options);
            }
        }
        else
        {
            dialogs.Add(RoleDialogModel.From(message,
                role: AgentRole.Assistant,
                content: message.Content));
            Context.SetDialogs(dialogs);
        }

        return true;
    }
}
