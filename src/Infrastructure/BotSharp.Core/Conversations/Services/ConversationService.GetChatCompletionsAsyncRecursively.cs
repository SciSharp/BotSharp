using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    int currentRecursiveDepth = 0;

    private async Task<bool> GetChatCompletionsAsyncRecursively(IChatCompletion chatCompletion,
        string conversationId,
        Agent agent, 
        List<RoleDialogModel> wholeDialogs,
        int maxRecursiveDepth,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting,
        Func<RoleDialogModel, Task> onFunctionExecuted)
    {
        currentRecursiveDepth++;
        if (currentRecursiveDepth > maxRecursiveDepth)
        {
            _logger.LogWarning($"Exceeded max recursive depth.");

            var latestResponse = wholeDialogs.Last();
            var text = latestResponse.Content;
            if (latestResponse.Role == AgentRole.Function)
            {
                text = latestResponse.Content.Split("=>").Last();
            }

            await HandleAssistantMessage(new RoleDialogModel(AgentRole.Assistant, text)
            {
                CurrentAgentId = agent.Id,
                Channel = wholeDialogs.Last().Channel
            }, onMessageReceived);

            return false;
        }

        var result = await chatCompletion.GetChatCompletionsAsync(agent, wholeDialogs, async msg =>
        {
            await HandleAssistantMessage(msg, onMessageReceived);

            // Add to dialog history
            _storage.Append(conversationId, agent.Id, msg);
        }, async fn =>
        {
            var preAgentId = agent.Id;

            await HandleFunctionMessage(fn, onFunctionExecuting, onFunctionExecuted);

            // Function executed has exception
            if (fn.ExecutionResult == null)
            {
                await HandleAssistantMessage(new RoleDialogModel(AgentRole.Assistant, fn.Content)
                {
                    CurrentAgentId = fn.CurrentAgentId,
                    Channel = fn.Channel
                }, onMessageReceived);
                return;
            }

            fn.Content = fn.FunctionArgs.Replace("\r", " ").Replace("\n", " ").Trim() + " => " + fn.ExecutionResult;

            // Agent has been transferred
            var agentSettings = _services.GetRequiredService<AgentSettings>();
            if (fn.CurrentAgentId != preAgentId)
            {
                var agentService = _services.GetRequiredService<IAgentService>();
                agent = await agentService.LoadAgent(fn.CurrentAgentId);
            }

            // Add to dialog history
            // The server had an error processing your request. Sorry about that!
            // _storage.Append(conversationId, preAgentId, fn);

            // After function is executed, pass the result to LLM to get a natural response
            wholeDialogs.Add(fn);

            await GetChatCompletionsAsyncRecursively(chatCompletion, 
                conversationId, 
                agent, 
                wholeDialogs, 
                maxRecursiveDepth,
                onMessageReceived, 
                onFunctionExecuting,
                onFunctionExecuted);
        });

        return result;
    }

    private async Task HandleAssistantMessage(RoleDialogModel msg, Func<RoleDialogModel, Task> onMessageReceived)
    {
        var hooks = _services.GetServices<IConversationHook>().ToList();

        // After chat completion hook
        foreach (var hook in hooks)
        {
            await hook.AfterCompletion(msg);
        }

        await onMessageReceived(msg);
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
