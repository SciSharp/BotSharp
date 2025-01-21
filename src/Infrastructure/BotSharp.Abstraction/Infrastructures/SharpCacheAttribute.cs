using BotSharp.Abstraction.Infrastructures;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Rougamo;
using Rougamo.Context;

namespace BotSharp.Core.Infrastructures;

public class SharpCacheAttribute : AsyncMoAttribute
{
    public static IServiceProvider Services { get; set; } = null!;
    private static readonly object NullMarker = new { __is_null = "$_is_null" };

    private int _minutes;

    public SharpCacheAttribute(int minutes = 60)
    {
        _minutes = minutes;
    }

    public override async ValueTask OnEntryAsync(MethodContext context)
    {

        var settings = Services.GetRequiredService<SharpCacheSettings>();
        if (!settings.Enabled)
        {
            return;
        }

        var cache = Services.GetRequiredService<ICacheService>();
        var key = GetCacheKey(settings, context);
        var value = await cache.GetAsync(key, context.TaskReturnType);
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

    public override async ValueTask OnSuccessAsync(MethodContext context)
    {
        var settings = Services.GetRequiredService<SharpCacheSettings>();
        if (!settings.Enabled)
        {
            return;
        }

        var httpContext = Services.GetRequiredService<IHttpContextAccessor>();
        if (httpContext != null &&
            httpContext.HttpContext != null &&
            httpContext.HttpContext.Response.Headers["Cache-Control"].ToString().Contains("no-store"))
        {
            return;
        }

        var cache = Services.GetRequiredService<ICacheService>();

        if (context.ReturnValue != null)
        {
            var key = GetCacheKey(settings, context);
            await cache.SetAsync(key, context.ReturnValue, new TimeSpan(0, _minutes, 0));
        }
    }

    public virtual Task<bool> IsOutOfDate(MethodContext context, object value)
    {
        return Task.FromResult(false);
    }


    private string GetCacheKey(SharpCacheSettings settings, MethodContext context)
    {
        var prefixKey = settings.Prefix + ":" + context.Method.Name;
        return $"{prefixKey}_{string.Join("_", context.Arguments.Select(arg => GetCacheKey(arg)))}";
    }

    private string GetCacheKey(object? arg)
    {
        if (arg is null)
        {
            return NullMarker.GetHashCode().ToString();
        }
        else if (arg is ICacheKey withCacheKey)
        { 
            return withCacheKey.GetCacheKey();
        }
        else
        {
            return arg.GetHashCode().ToString();
        }
    }
}
