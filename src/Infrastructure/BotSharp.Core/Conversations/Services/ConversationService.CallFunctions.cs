using BotSharp.Abstraction.Functions;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService
{
    public async Task CallFunctions(RoleDialogModel msg)
    {
        var hooks = _services.GetServices<IConversationHook>()
            .OrderBy(x => x.Priority)
            .ToList();

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
            }
            catch (Exception ex)
            {
                msg.ExecutionResult = ex.Message;
                _logger.LogError(msg.ExecutionResult);
            }
            
            // After functions have been executed
            foreach (var hook in hooks)
            {
                await hook.OnFunctionExecuted(msg);
            }
        }
    }
}
