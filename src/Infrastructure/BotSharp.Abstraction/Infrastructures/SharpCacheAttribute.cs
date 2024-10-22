using BotSharp.Abstraction.Infrastructures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Rougamo;
using Rougamo.Context;

namespace BotSharp.Core.Infrastructures;

public class SharpCacheAttribute : MoAttribute
{
    public static IServiceProvider Services { get; set; } = null!;

    private int _minutes;

    public SharpCacheAttribute(int minutes = 60)
    {
        _minutes = minutes;
    }

    public override void OnEntry(MethodContext context)
    {
        var settings = Services.GetRequiredService<SharpCacheSettings>();
        if (!settings.Enabled)
        {
            return;
        }

        var cache = Services.GetRequiredService<ICacheService>();

        var key = GetCacheKey(settings, context);
        var value = cache.GetAsync(key, context.TaskReturnType).Result;
        if (value != null)
        {
            // check if the cache is out of date
            var isOutOfDate = IsOutOfDate(context, value).Result;

            if (!isOutOfDate)
            {
                context.ReplaceReturnValue(this, value);
            }
        }
    }

    public override void OnSuccess(MethodContext context)
    {
        var settings = Services.GetRequiredService<SharpCacheSettings>();
        if (!settings.Enabled)
        {
            return;
        }

        var httpContext = Services.GetRequiredService<IHttpContextAccessor>();
        if (httpContext.HttpContext.Response.Headers["Cache-Control"].ToString().Contains("no-store"))
        {
            return;
        }

        var cache = Services.GetRequiredService<ICacheService>();

        if (context.ReturnValue != null)
        {
            var key = GetCacheKey(settings, context);
            cache.SetAsync(key, context.ReturnValue, new TimeSpan(0, _minutes, 0)).Wait();
        }
    }

    public virtual Task<bool> IsOutOfDate(MethodContext context, object value)
    {
        return Task.FromResult(false);
    }

    private string GetCacheKey(SharpCacheSettings settings, MethodContext context)
    {
        var key = settings.Prefix + ":" + context.Method.Name;
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
