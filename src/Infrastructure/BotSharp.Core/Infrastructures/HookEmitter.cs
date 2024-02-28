using BotSharp.Abstraction.Infrastructures;

namespace BotSharp.Core.Infrastructures;

public static class HookEmitter
{
    public static async Task<HookEmittedResult> Emit<T>(IServiceProvider services, Action<T> action)
    {
        var logger = services.GetRequiredService<ILogger<T>>();
        var result = new HookEmittedResult();
        var hooks = services.GetServices<T>();

        foreach (var hook in hooks)
        {
            try
            {
                action(hook);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
        }

        return result;
    }
}
