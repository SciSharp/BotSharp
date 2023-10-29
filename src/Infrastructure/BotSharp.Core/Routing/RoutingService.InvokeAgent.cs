using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    const int MAXIMUM_RECURSION_DEPTH = 3;
    private int _currentRecursionDepth = 0;
    public async Task<bool> InvokeAgent(string agentId, RoleDialogModel message)
    {
        _currentRecursionDepth++;
        if (_currentRecursionDepth > MAXIMUM_RECURSION_DEPTH)
        {
            _logger.LogWarning($"Current recursive call depth greater than {MAXIMUM_RECURSION_DEPTH}, which will cause unexpected result.");
            return false;
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        var settings = _services.GetRequiredService<ChatCompletionSetting>();
        var chatCompletion = CompletionProvider.GetChatCompletion(_services, provider: settings.Provider, model: settings.Model);
        RoleDialogModel response = chatCompletion.GetChatCompletions(agent, Dialogs);
        message.Role = response.Role;

        if (response.Role == AgentRole.Function)
        {
            message.FunctionName = response.FunctionName;
            message.FunctionArgs = response.FunctionArgs;

            await InvokeFunction(agent, message);
        }
        else
        {
            message.Content = response.Content;
        }

        return true;
    }

    private async Task<RoleDialogModel> InvokeFunction(Agent agent, RoleDialogModel message)
    {
        // execute function
        // Save states
        SaveStateByArgs(JsonSerializer.Deserialize<JsonDocument>(message.FunctionArgs));

        var conversationService = _services.GetRequiredService<IConversationService>();
        // Call functions
        await conversationService.CallFunctions(message);

        Dialogs.Add(message);

        // Pass execution result to LLM to get response
        if (!message.StopCompletion)
        {
            // Find response template
            var templateService = _services.GetRequiredService<IResponseTemplateService>();
            var responseTemplate = await templateService.RenderFunctionResponse(agent.Id, message);
            if (!string.IsNullOrEmpty(responseTemplate))
            {
                message.Role = AgentRole.Assistant;
                message.Content = responseTemplate.Trim();
            }
            else
            {
                await InvokeAgent(agent.Id, message);
            }
        }
        else
        {
            message.Role = AgentRole.Assistant;
        }

        return message;
    }
}
