using BotSharp.Abstraction.Infrastructures;
using Microsoft.Extensions.DependencyInjection;
using Rougamo;
using Rougamo.Context;

namespace BotSharp.Core.Infrastructures;

public class SharpCacheAttribute : MoAttribute
{
    public static IServiceProvider Services { get; set; } = null!;

    private int _minutes;
    private string _prefix;

    public SharpCacheAttribute(int minutes, string prefix = "cache")
    {
        _minutes = minutes;
        _prefix = prefix;
    }

    public override void OnEntry(MethodContext context)
    {
        var cache = Services.GetRequiredService<ICacheService>();

        var key = GetCacheKey(context);
        var value = cache.GetAsync(key, context.TaskReturnType).Result;
        if (value != null)
        {
            context.ReplaceReturnValue(this, value);
        }
    }

    public override void OnSuccess(MethodContext context)
    {
        var cache = Services.GetRequiredService<ICacheService>();

        if (context.ReturnValue != null)
        {
            var key = GetCacheKey(context);
            cache.SetAsync(key, context.ReturnValue, new TimeSpan(0, _minutes, 0)).Wait();
        }
    }

    private string GetCacheKey(MethodContext context)
    {
        var key = _prefix;
        foreach (var arg in context.Arguments)
        {
            if (arg is null)
            {
                key += "-" + "<NULL>";
            }
            else if (arg is ICacheKey withCacheKey)
            {
                key += "-" + withCacheKey.GetCacheKey();
            }
            else
            {
                key += "-" + arg.ToString();
            }
        }

        return key;
    }
}
