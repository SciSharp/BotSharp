using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Infrastructures;

namespace BotSharp.Core.Infrastructures;

public static class HookEmitter
{
    public static HookEmittedResult Emit<T>(IServiceProvider services, Action<T> action, string agentId, HookEmitOption<T>? option = null) where T : IHookBase
    {
        var logger = services.GetRequiredService<ILogger<T>>();
        var result = new HookEmittedResult();
        var hooks = services.GetHooks<T>(agentId);
        option = option ?? new();

        foreach (var hook in hooks)
        {
            try
            {
                if (option.ShouldExecute == null || option.ShouldExecute(hook))
                {
                    logger.LogDebug($"Emit hook action on {action.Method.Name}({hook.GetType().Name})");
                    action(hook);

                    if (option.OnlyOnce)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        return result;
    }

    public static async Task<HookEmittedResult> Emit<T>(IServiceProvider services, Func<T, Task> action, string agentId, HookEmitOption<T>? option = null) where T : IHookBase
    {
        var logger = services.GetRequiredService<ILogger<T>>();
        var result = new HookEmittedResult();
        var hooks = services.GetHooks<T>(agentId);
        option = option ?? new();

        foreach (var hook in hooks)
        {
            try
            {
                if (option.ShouldExecute == null || option.ShouldExecute(hook))
                {
                    logger.LogDebug($"Emit hook action on {action.Method.Name}({hook.GetType().Name})");
                    await action(hook);

                    if (option.OnlyOnce)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        return result;
    }
}
