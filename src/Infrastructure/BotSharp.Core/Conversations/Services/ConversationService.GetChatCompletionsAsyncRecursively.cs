using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    int currentRecursiveDepth = 0;

    private async Task<bool> GetChatCompletionsAsyncRecursively(Agent agent, 
        List<RoleDialogModel> wholeDialogs,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting,
        Func<RoleDialogModel, Task> onFunctionExecuted)
    {
        var chatCompletion = CompletionProvider.GetChatCompletion(_services);

        currentRecursiveDepth++;
        if (currentRecursiveDepth > _settings.MaxRecursiveDepth)
        {
            _logger.LogWarning($"Exceeded max recursive depth.");

            var latestResponse = wholeDialogs.Last();
            var text = latestResponse.Content;
            if (latestResponse.Role == AgentRole.Function)
            {
                text = latestResponse.Content.Split("=>").Last();
            }

            await HandleAssistantMessage(agent, new RoleDialogModel(AgentRole.Assistant, text)
            {
                CurrentAgentId = agent.Id
            }, onMessageReceived);

            return false;
        }

        var result = await chatCompletion.GetChatCompletionsAsync(agent, wholeDialogs, async msg =>
        {
            await HandleAssistantMessage(agent, msg, onMessageReceived);
        }, async fn =>
        {
            var preAgentId = agent.Id;

            await HandleFunctionMessage(fn, onFunctionExecuting, onFunctionExecuted);

            // Function executed has exception
            if (fn.ExecutionResult == null)
            {
                await HandleAssistantMessage(agent, new RoleDialogModel(AgentRole.Assistant, fn.Content)
                {
                    CurrentAgentId = fn.CurrentAgentId
                }, onMessageReceived);

                return;
            }
            else if (fn.StopCompletion)
            {
                await HandleAssistantMessage(agent, new RoleDialogModel(AgentRole.Assistant, fn.Content)
                {
                    CurrentAgentId = fn.CurrentAgentId,
                    ExecutionData = fn.ExecutionData,
                    ExecutionResult = fn.ExecutionResult
                }, onMessageReceived);

                return;
            }

            var content = fn.FunctionArgs.Replace("\r", " ").Replace("\n", " ").Trim() + " => " + fn.ExecutionResult;
            _logger.LogInformation(content);

            fn.Content = content;

            // Agent has been transferred
            if (fn.CurrentAgentId != preAgentId)
            {
                var agentService = _services.GetRequiredService<IAgentService>();
                agent = await agentService.LoadAgent(fn.CurrentAgentId);

                if (fn.FunctionName != "route_to_agent")
                {
                    wholeDialogs.Add(fn);
                }

                await GetChatCompletionsAsyncRecursively(agent,
                    wholeDialogs,
                    onMessageReceived,
                    onFunctionExecuting,
                    onFunctionExecuted);
            }
            else
            {
                // Find response template
                var templateService = _services.GetRequiredService<IResponseTemplateService>();
                var response = await templateService.RenderFunctionResponse(agent.Id, fn);
                if (!string.IsNullOrEmpty(response))
                {
                    await HandleAssistantMessage(agent, new RoleDialogModel(AgentRole.Assistant, response)
                    {
                        CurrentAgentId = agent.Id
                    }, onMessageReceived);

                    return;
                }

                // Add to dialog history
                // The server had an error processing your request. Sorry about that!
                // _storage.Append(conversationId, preAgentId, fn);

                // After function is executed, pass the result to LLM to get a natural response
                if (fn.FunctionName != "route_to_agent")
                {
                    wholeDialogs.Add(fn);
                }

                await GetChatCompletionsAsyncRecursively(agent,
                    wholeDialogs,
                    onMessageReceived,
                    onFunctionExecuting,
                    onFunctionExecuted);
            }
        });

        return result;
    }

    private async Task HandleAssistantMessage(Agent agent, RoleDialogModel message, Func<RoleDialogModel, Task> onMessageReceived)
    {
        var hooks = _services.GetServices<IConversationHook>().ToList();

        // After chat completion hook
        foreach (var hook in hooks)
        {
            await hook.AfterCompletion(message);
        }

        _logger.LogInformation($"[{agent.Name}] {message.Role}: {message.Content}");

        await onMessageReceived(message);

        // Add to dialog history
        _storage.Append(_conversationId, message);
    }

    private async Task HandleFunctionMessage(RoleDialogModel msg, 
        Func<RoleDialogModel, Task> onFunctionExecuting,
        Func<RoleDialogModel, Task> onFunctionExecuted)
    {
        // Save states
        SaveStateByArgs(msg.FunctionArgs);

        // Call functions
        await onFunctionExecuting(msg);
        await CallFunctions(msg);
        await onFunctionExecuted(msg);
    }
}
