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

        var funcExecutor = FunctionExecutorFactory.Create(_services, name, agent);
        if (funcExecutor == null)
        {
            message.StopCompletion = true;
            message.Content = $"Can't find function implementation of {name}.";
            _logger.LogError(message.Content);
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
            result = await funcExecutor.ExecuteAsync(clonedMessage);

            // After functions have been executed
            foreach (var hook in hooks)
            {
                await hook.OnFunctionExecuted(clonedMessage, options);
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
            message.AdditionalMessageWrapper = clonedMessage.AdditionalMessageWrapper;
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
