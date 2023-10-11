using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    const int MAXIMUM_RECURSION_DEPTH = 3;
    int CurrentRecursionDepth = 0;
    public async Task<RoleDialogModel> InvokeAgent(string agentId)
    {
        CurrentRecursionDepth++;
        if (CurrentRecursionDepth > MAXIMUM_RECURSION_DEPTH)
        {
            return Dialogs.Last();
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(agentId);

        var chatCompletion = CompletionProvider.GetChatCompletion(_services);
        RoleDialogModel response = chatCompletion.GetChatCompletions(agent, Dialogs);

        if (response.Role == AgentRole.Function)
        {
            await InvokeFunction(agent, response);
        }

        return response;
    }

    private async Task InvokeFunction(Agent agent, RoleDialogModel response)
    {
        // execute function
        // Save states
        SaveStateByArgs(JsonSerializer.Deserialize<JsonDocument>(response.FunctionArgs));

        var conversationService = _services.GetRequiredService<IConversationService>();
        // Call functions
        await conversationService.CallFunctions(response);

        if (string.IsNullOrEmpty(response.Content))
        {
            response.Content = response.ExecutionResult;
        }

        Dialogs.Add(response);

        if (!response.StopCompletion)
        {
            // Find response template
            var templateService = _services.GetRequiredService<IResponseTemplateService>();
            var responseTemplate = await templateService.RenderFunctionResponse(agent.Id, response);
            if (!string.IsNullOrEmpty(responseTemplate))
            {
                response.Role = AgentRole.Assistant;
                response.Content = responseTemplate;
            }
            else
            {
                var recursiveResponse = await InvokeAgent(response.CurrentAgentId);
                response.Role = recursiveResponse.Role;
                response.Content = recursiveResponse.Content;
                response.ExecutionResult = recursiveResponse.ExecutionResult;
                response.ExecutionData = recursiveResponse.ExecutionData ?? response.ExecutionData;
                response.StopCompletion = recursiveResponse.StopCompletion;
            }
        }
    }
}
