using BotSharp.Abstraction.Conversations;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Abstraction.Hooks;

public static class HookProvider    
{
    public static List<T> GetHooks<T>(this IServiceProvider services, string agentId) where T : IHookBase
    {
        var hooks = services.GetServices<T>().Where(p => p.IsMatch(agentId));
        return hooks.ToList();
    }

    public static List<T> GetHooksOrderByPriority<T>(this IServiceProvider services, string agentId) where T: IConversationHook
    {
        var hooks = services.GetServices<T>().Where(p => p.IsMatch(agentId));
        return hooks.OrderBy(p => p.Priority).ToList();
    }
}
