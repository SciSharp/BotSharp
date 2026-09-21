using BotSharp.Abstraction.Routing.Models;
using BotSharp.Abstraction.Templating;
using BotSharp.Abstraction.Utilities;

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
        var conversationDialogs = dialogs.Where(x => !x.ExcludeFromContext).ToList();
        if (options?.UseStream == true)
        {
            response = await chatCompletion.GetChatCompletionsStreamingAsync(agent, conversationDialogs);
        }
        else
        {
            response = await chatCompletion.GetChatCompletions(agent, conversationDialogs);
        }

        var toolCalls = GetToolCalls(response);
        if (!toolCalls.IsNullOrEmpty())
        {
            await InvokeToolCalls(toolCalls, response, message, agent, dialogs, options);
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
            message.Thought = response.Thought != null ? new(response.Thought) : null;
            message.MetaData = response.MetaData != null ? new(response.MetaData) : null;
            message.IsStreaming = response.IsStreaming;
            message.MessageLabel = response.MessageLabel;
            dialogs.Add(message);
            Context.AddDialogs([message]);
        }

        return true;
    }

    /// <summary>
    /// Every tool call one reply asked for, in the order the model produced them.
    /// </summary>
    /// <remarks>
    /// <see cref="RoleDialogModel.ToolCalls"/> is where a provider reports the whole set. The
    /// single FunctionName/FunctionArgs/ToolCallId fields beside it are a view of its first entry,
    /// kept for callers that can only run one call, so a provider that fills only those is read
    /// through the second branch and needs no change to work here.
    /// </remarks>
    private static List<LlmToolCall> GetToolCalls(RoleDialogModel response)
    {
        if (!response.ToolCalls.IsNullOrEmpty())
        {
            // A call with no name is not something that can be dispatched. The single-call path
            // has always guarded on the name being present, and the batch drops it the same way
            // rather than handing an empty name to the executor factory.
            return response.ToolCalls!
                .Where(x => !string.IsNullOrEmpty(x.FunctionName))
                .ToList();
        }

        if (response.Role == AgentRole.Function && !string.IsNullOrEmpty(response.FunctionName))
        {
            return [new LlmToolCall(response.ToolCallId, response.FunctionName, response.FunctionArgs)];
        }

        return [];
    }

    /// <summary>
    /// Runs every call the reply asked for, in order, and then hands the whole set of results back
    /// to the model in one round.
    /// </summary>
    /// <remarks>
    /// The recursion into <see cref="InvokeAgent"/> belongs here rather than inside
    /// <see cref="InvokeFunction"/>: a per-call recursion spends the turn on the first call, and
    /// the calls after it never run at all. Deciding once for the whole batch is what lets a model
    /// ask for three independent lookups and get three answers in a single round trip.
    /// </remarks>
    private async Task InvokeToolCalls(
        List<LlmToolCall> toolCalls,
        RoleDialogModel response,
        RoleDialogModel source,
        Agent agent,
        List<RoleDialogModel> dialogs,
        InvokeAgentOptions? options)
    {
        if (toolCalls.Count > 1)
        {
            // Worth a line of its own. Until now a reply like this ran one call and dropped the
            // rest, so an agent whose prompt was tuned against that behaviour changes the moment
            // this ships. This is where to look when one does.
            _logger.LogInformation($"Agent {agent.Name} asked for {toolCalls.Count} tool calls in one reply: " +
                $"{string.Join(", ", toolCalls.Select(x => x.FunctionName))}");
        }

        var completed = true;

        for (var i = 0; i < toolCalls.Count; i++)
        {
            var toolCall = toolCalls[i];

            // Each call in the batch answers the same message. They are siblings of one reply, not
            // a chain, so every one is built from the dialog that reply responded to rather than
            // from the result the previous call just appended.
            var message = RoleDialogModel.From(source, role: AgentRole.Function);
            message.ToolCallId = toolCall.Id;
            message.FunctionName = toolCall.FunctionName.NormalizeFunctionName();
            message.FunctionArgs = toolCall.FunctionArgs;
            message.Thought = response.Thought != null ? new(response.Thought) : null;
            message.MetaData = response.MetaData != null ? new(response.MetaData) : null;
            message.Indication = response.Indication;
            message.CurrentAgentId = agent.Id;
            message.IsStreaming = response.IsStreaming;
            message.MessageLabel = response.MessageLabel;

            completed = await InvokeFunction(message, dialogs, options);
            if (!completed)
            {
                var skipped = toolCalls.Count - i - 1;
                if (skipped > 0)
                {
                    // The model asked for these without knowing an earlier call would end the
                    // turn. Running them into a finished turn produces results nothing reads, so
                    // they are dropped -- but not silently, which is how everything beyond the
                    // first call used to disappear.
                    _logger.LogInformation($"{message.FunctionName} ended the turn, skipping the remaining {skipped} tool call(s) of this reply: " +
                        $"{string.Join(", ", toolCalls.Skip(i + 1).Select(x => x.FunctionName))}");
                }
                break;
            }
        }

        if (completed)
        {
            // One round trip for the whole batch: the model sees every result at once. The agent
            // is read after the last call because a routing tool may have changed it.
            var routing = _services.GetRequiredService<IRoutingService>();
            var curAgentId = routing.Context.GetCurrentAgentId();
            await InvokeAgent(curAgentId, dialogs, options);
        }
    }

    /// <summary>
    /// Executes one tool call and appends its result to the dialogs.
    /// </summary>
    /// <returns>
    /// Whether the turn should continue. False when this call ended it -- either the function set
    /// <see cref="RoleDialogModel.StopCompletion"/> and answered the user itself, or a response
    /// template answered in its place. Both have always ended the turn; what is new is that the
    /// caller is the one told about it, instead of this method deciding by recursing or not.
    /// </returns>
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

        if (message.StopCompletion)
        {
            // The function wrote the reply itself, and the assistant message that follows repeats
            // its text. The call is recorded anyway, and stays in context: it is the only thing
            // telling a later turn that this function already ran.
            await Persist(RoleDialogModel.From(message, role: AgentRole.Function));

            var msg = RoleDialogModel.From(message,
                role: AgentRole.Assistant,
                content: message.Content);
            dialogs.Add(msg);
            Context.AddDialogs([msg]);

            return false;
        }

        // Find response template
        var templateService = _services.GetRequiredService<IResponseTemplateService>();
        var responseTemplate = await templateService.RenderFunctionResponse(message.CurrentAgentId, message);
        if (!string.IsNullOrEmpty(responseTemplate))
        {
            var msg = RoleDialogModel.From(message,
                role: AgentRole.Assistant,
                content: responseTemplate);
            dialogs.Add(msg);
            Context.AddDialogs([msg]);

            return false;
        }

        // Save to memory dialogs and to storage
        var functionMsg = RoleDialogModel.From(message,
            role: AgentRole.Function,
            content: message.Content);

        dialogs.Add(functionMsg);
        Context.AddDialogs([functionMsg]);
        await Persist(functionMsg);

        return true;
    }

    private async Task Persist(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        if (!conv.IsConversationMode())
        {
            return;
        }

        var storage = _services.GetRequiredService<IConversationStorage>();
        await storage.Append(conv.ConversationId, message);
    }
}
