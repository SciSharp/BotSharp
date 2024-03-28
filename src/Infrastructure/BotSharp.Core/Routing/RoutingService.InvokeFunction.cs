using BotSharp.Abstraction.Functions;

namespace BotSharp.Core.Routing;

public partial class RoutingService
{
    public async Task<bool> InvokeFunction(string name, RoleDialogModel message)
    {
        var function = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name == name);
        if (function == null)
        {
            message.StopCompletion = true;
            message.Content = $"Can't find function implementation of {message.FunctionName}.";
            _logger.LogError(message.Content);
            return false;
        }

        var originalFunctionName = message.FunctionName;
        message.FunctionName = name;
        message.Role = AgentRole.Function;
        message.FunctionArgs = message.FunctionArgs;

        var hooks = _services.GetServices<IConversationHook>()
            .OrderBy(x => x.Priority)
            .ToList();

        // Before executing functions
        foreach (var hook in hooks)
        {
            await hook.OnFunctionExecuting(message);
        }

        bool result = false;

        try
        {
            result = await function.Execute(message);
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

        // After functions have been executed
        foreach (var hook in hooks)
        {
            await hook.OnFunctionExecuted(message);
        }

        // restore original function name
        if (!message.StopCompletion && 
            message.FunctionName != originalFunctionName)
        {
            message.FunctionName = originalFunctionName;
        }

        // Save to Storage as well
        if (!message.StopCompletion && message.FunctionName != "route_to_agent")
        {
            var storage = _services.GetRequiredService<IConversationStorage>();
            storage.Append(Context.ConversationId, message);
        }

        return result;
    }
}
