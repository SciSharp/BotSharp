using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    const int MAXIMUM_RECURSION_DEPTH = 3;
    private int _currentRecursionDepth = 0;
    public async Task<RoleDialogModel> InvokeAgent(string agentId)
    {
        _currentRecursionDepth++;
        if (_currentRecursionDepth > MAXIMUM_RECURSION_DEPTH)
        {
            _logger.LogWarning($"Current recursive call depth greater than {MAXIMUM_RECURSION_DEPTH}, which will cause unexpected result.");
            return Dialogs.Last();
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        var settings = _services.GetRequiredService<ChatCompletionSetting>();
        var chatCompletion = CompletionProvider.GetChatCompletion(_services, provider: settings.Provider, model: settings.Model);
        RoleDialogModel response = chatCompletion.GetChatCompletions(agent, Dialogs);

        if (response.Role == AgentRole.Function)
        {
            return await InvokeFunction(agent, response);
        }
        else
        {
            return response;
        }
    }

    private async Task<RoleDialogModel> InvokeFunction(Agent agent, RoleDialogModel response)
    {
        // execute function
        // Save states
        SaveStateByArgs(JsonSerializer.Deserialize<JsonDocument>(response.FunctionArgs));

        var conversationService = _services.GetRequiredService<IConversationService>();
        // Call functions
        await conversationService.CallFunctions(response);

        Dialogs.Add(response);

        // Pass execution result to LLM to get response
        if (!response.StopCompletion)
        {
            // Find response template
            var templateService = _services.GetRequiredService<IResponseTemplateService>();
            var responseTemplate = await templateService.RenderFunctionResponse(agent.Id, response);
            if (!string.IsNullOrEmpty(responseTemplate))
            {
                response.Role = AgentRole.Assistant;
                response.Content = responseTemplate.Trim();
            }
            else
            {
                response = await InvokeAgent(response.CurrentAgentId);
            }
        }
        else
        {
            response.Role = AgentRole.Assistant;
        }

        return response;
    }
}
