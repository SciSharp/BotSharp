using BotSharp.Abstraction.Infrastructures;

namespace BotSharp.Core.Infrastructures;

public static class HookEmitter
{
    public static HookEmittedResult Emit<T>(IServiceProvider services, Action<T> action)
    {
        var logger = services.GetRequiredService<ILogger<T>>();
        var result = new HookEmittedResult();
        var hooks = services.GetServices<T>();

        foreach (var hook in hooks)
        {
            try
            {
                logger.LogInformation($"Emit hook action on {action.Method.Name}({hook.GetType().Name})");
                action(hook);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        return result;
    }

    public static async Task<HookEmittedResult> Emit<T>(IServiceProvider services, Func<T, Task> action)
    {
        var logger = services.GetRequiredService<ILogger<T>>();
        var result = new HookEmittedResult();
        var hooks = services.GetServices<T>();

        foreach (var hook in hooks)
        {
            try
            {
                logger.LogInformation($"Emit hook action on {action.Method.Name}({hook.GetType().Name})");
                await action(hook);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        return result;
    }
}
