using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    const int MAXIMUM_RECURSION_DEPTH = 2;
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

        RoleDialogModel response = null;
        await chatCompletion.GetChatCompletionsAsync(agent, Dialogs,
            async msg =>
            {
                response = msg;
            }, async fn =>
            {
                // execute function
                // Save states
                SaveStateByArgs(JsonSerializer.Deserialize<JsonDocument>(fn.FunctionArgs));

                var conversationService = _services.GetRequiredService<IConversationService>();
                // Call functions
                await conversationService.CallFunctions(fn);

                if (string.IsNullOrEmpty(fn.Content))
                {
                    fn.Content = fn.ExecutionResult;
                }

                Dialogs.Add(fn);

                if (!fn.StopCompletion)
                {
                    // Find response template
                    var templateService = _services.GetRequiredService<IResponseTemplateService>();
                    var quickResponse = await templateService.RenderFunctionResponse(agent.Id, fn);
                    if (!string.IsNullOrEmpty(quickResponse))
                    {
                        response = new RoleDialogModel(AgentRole.Assistant, quickResponse)
                        {
                            CurrentAgentId = agent.Id
                        };
                    }
                    else
                    {
                        response = await InvokeAgent(fn.CurrentAgentId);
                    }
                }
                else
                {
                    response = fn;
                }
            });

        return response;
    }
}
