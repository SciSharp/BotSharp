using BotSharp.Abstraction.Routing.Executor;
using BotSharp.Abstraction.Routing.Models;
using BotSharp.Core.MessageHub;
using BotSharp.Core.Routing.Executor;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<bool> InvokeFunction(string name, RoleDialogModel message, InvokeFunctionOptions? options = null)
    {
        options ??= InvokeFunctionOptions.Default();
        var currentAgentId = message.CurrentAgentId;
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.GetAgent(currentAgentId);

        var funcExecutor = _services.GetRequiredService<IFunctionExecutorFactory>().Create(name, agent);
        if (funcExecutor == null)
        {
            message.StopCompletion = true;
            message.Content = $"Can't find function implementation of {name}.";
            _logger.LogError($"{message.Content}, stackInfo:{DiagnosticHelper.GetCurrentStackTrace()}");
            return false;
        }

        // Clone message
        var clonedMessage = RoleDialogModel.From(message);
        clonedMessage.FunctionName = name;
        clonedMessage.Indication = await funcExecutor.GetIndicatorAsync(message);

        var conv = _services.GetRequiredService<IConversationService>();
        var messageHub = _services.GetRequiredService<MessageHub<HubObserveData<RoleDialogModel>>>();
        messageHub.Push(new()
        {
            EventName = ChatEvent.OnIndicationReceived,
            Data = clonedMessage,
            RefId = conv.ConversationId
        });

        var hooks = _services.GetHooksOrderByPriority<IConversationHook>(clonedMessage.CurrentAgentId);
        foreach (var hook in hooks)
        {
            hook.SetAgent(agent);
            await hook.OnFunctionExecuting(clonedMessage, options);
        }

        bool result = false;

        try
        {
            // A before-hook may REFUSE the call outright by setting Handled, which is what that
            // flag has always been documented to mean on RoleDialogModel — it was simply never
            // read here, so a hook that decided a tool must not run watched it run anyway.
            //
            // The refusal's own words are already on the cloned message and are copied back below
            // as the tool's result, so the model reads why it was refused rather than a silent
            // no-op. The executed-hooks are skipped with the execution: they exist to react to
            // what a tool DID, and a call that never happened did nothing for them to record.
            if (!clonedMessage.Handled)
            {
                result = await funcExecutor.ExecuteAsync(clonedMessage);

                // After functions have been executed
                foreach (var hook in hooks)
                {
                    await hook.OnFunctionExecuted(clonedMessage, options);
                }
            }

            // Set result to original message
            message.Role = clonedMessage.Role;
            message.PostbackFunctionName = clonedMessage.PostbackFunctionName;
            message.CurrentAgentId = clonedMessage.CurrentAgentId;
            message.Content = clonedMessage.Content;
            // Don't copy payload
            // message.Payload = clonedMessage.Payload;
            message.StopCompletion = clonedMessage.StopCompletion;
            message.RichContent = clonedMessage.RichContent;
            message.Data = clonedMessage.Data;
            message.MessageLabel = clonedMessage.MessageLabel;
            message.Handled = clonedMessage.Handled;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, $"The input does not contain any JSON tokens:\r\n{message.Content}\r\n{ex.Message}");
            message.StopCompletion = true;
            message.Content = ex.Message;
        }
        catch (Exception ex)
        {
            message.StopCompletion = true;
            message.Content = ex.Message;
            _logger.LogError(ex, ex.ToString());
        }

        // Make sure content has been populated
        if (string.IsNullOrEmpty(message.Content) && message.Data != null)
        {
            message.Content = JsonSerializer.Serialize(message.Data);
        }

        return result;
    }
}
