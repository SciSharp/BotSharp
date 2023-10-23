using BotSharp.Abstraction.Functions;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    public async Task CallFunctions(RoleDialogModel msg)
    {
        // Invoke functions
        var functions = _services.GetServices<IFunctionCallback>()
            .Where(x => x.Name == msg.FunctionName)
            .ToList();

        if (functions.Count == 0)
        {
            msg.Content = $"Can't find function implementation of {msg.FunctionName}.";
            _logger.LogError(msg.Content);
            return;
        }

        var hooks = _services.GetServices<IConversationHook>()
            .OrderBy(x => x.Priority)
            .ToList();

        foreach (var fn in functions)
        {
            // Before executing functions
            foreach (var hook in hooks)
            {
                await hook.OnFunctionExecuting(msg);
            }

            try
            {
                // Execute function
                await fn.Execute(msg);

                if (string.IsNullOrEmpty(msg.Content))
                {
                    msg.Content = msg.Content ?? JsonSerializer.Serialize(msg.Data);
                }
            }
            catch (Exception ex)
            {
                msg.Content = ex.Message;
                _logger.LogError(msg.Content);
            }
            
            // After functions have been executed
            foreach (var hook in hooks)
            {
                await hook.OnFunctionExecuted(msg);
            }
        }
    }
}
