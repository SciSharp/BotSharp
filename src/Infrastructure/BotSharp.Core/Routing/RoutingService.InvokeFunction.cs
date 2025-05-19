using BotSharp.Abstraction.Hooks;
using BotSharp.Core.Routing.Executor;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<bool> InvokeFunction(string name, RoleDialogModel message)
    {
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

        var progressService = _services.GetService<IConversationProgressService>();
        clonedMessage.Indication = await funcExecutor.GetIndicatorAsync(message);

        if (progressService?.OnFunctionExecuting != null)
        {
            await progressService.OnFunctionExecuting(clonedMessage);
        }
        
        var hooks = _services.GetHooksOrderByPriority<IConversationHook>(clonedMessage.CurrentAgentId);
        foreach (var hook in hooks)
        {
            hook.SetAgent(agent);
            await hook.OnFunctionExecuting(clonedMessage);
        }

        bool result = false;

        try
        {
            result = await funcExecutor.ExecuteAsync(clonedMessage);

            // After functions have been executed
            foreach (var hook in hooks)
            {
                await hook.OnFunctionExecuted(clonedMessage);
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
        }
        catch (JsonException ex)
        {
            _logger.LogError($"The input does not contain any JSON tokens:\r\n{message.Content}");
            message.StopCompletion = true;
            message.Content = ex.Message;
        }
        catch (Exception ex)
        {
            message.StopCompletion = true;
            message.Content = ex.Message;
            _logger.LogError(ex.ToString());
        }

        // Make sure content has been populated
        if (string.IsNullOrEmpty(message.Content) && message.Data != null)
        {
            message.Content = JsonSerializer.Serialize(message.Data);
        }

        return result;
    }
}
