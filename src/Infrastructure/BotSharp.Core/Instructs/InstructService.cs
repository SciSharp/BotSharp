using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.MLTasks;
using BotSharp.Abstraction.Templating;

namespace BotSharp.Core.Instructs;

public partial class InstructService : IInstructService
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public InstructService(IServiceProvider services, ILogger<InstructService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> ExecuteInstructionRecursively(Agent agent,
        List<RoleDialogModel> wholeDialogs,
        Func<RoleDialogModel, Task> onMessageReceived, 
        Func<RoleDialogModel, Task> onFunctionExecuting, 
        Func<RoleDialogModel, Task> onFunctionExecuted)
    {
        var chatCompletion = GetChatCompletion();

        var result = await chatCompletion.GetChatCompletionsAsync(agent, wholeDialogs, async msg =>
        {
            await onMessageReceived(msg);
        }, async fn =>
        {
            var preAgentId = agent.Id;

            await HandleFunctionMessage(fn, onFunctionExecuting, onFunctionExecuted);

            // Function executed has exception
            if (fn.ExecutionResult == null || fn.StopCompletion)
            {
                await onMessageReceived(new RoleDialogModel(AgentRole.Assistant, fn.Content));
                return;
            }

            fn.Content = fn.FunctionArgs.Replace("\r", " ").Replace("\n", " ").Trim() + " => " + fn.ExecutionResult;

            // Find response template
            var templateService = _services.GetRequiredService<IResponseTemplateService>();
            var response = await templateService.RenderFunctionResponse(agent.Id, fn);
            if (!string.IsNullOrEmpty(response))
            {
                await onMessageReceived(new RoleDialogModel(AgentRole.Assistant, response));
                return;
            }

            // After function is executed, pass the result to LLM to get a natural response
            wholeDialogs.Add(fn);

            await ExecuteInstructionRecursively(agent,
                wholeDialogs,
                onMessageReceived,
                onFunctionExecuting,
                onFunctionExecuted);
        });

        return result;
    }

    private async Task HandleFunctionMessage(RoleDialogModel msg,
        Func<RoleDialogModel, Task> onFunctionExecuting,
        Func<RoleDialogModel, Task> onFunctionExecuted)
    {
        // Call functions
        await onFunctionExecuting(msg);
        await CallFunctions(msg);
        await onFunctionExecuted(msg);
    }

    public IChatCompletion GetChatCompletion()
    {
        var completions = _services.GetServices<IChatCompletion>();
        var settings = _services.GetRequiredService<ConversationSetting>();
        return completions.FirstOrDefault(x => x.GetType().FullName.EndsWith(settings.ChatCompletion));
    }
}
