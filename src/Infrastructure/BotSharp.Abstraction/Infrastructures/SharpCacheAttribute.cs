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

    private readonly int _minutes;
    private readonly bool _perInstanceCache;
    private readonly ICacheService _cache;
    private readonly SharpCacheSettings _settings;

    public SharpCacheAttribute(int minutes = 60, bool perInstanceCache = false)
    {
        _minutes = minutes;
        _perInstanceCache = perInstanceCache;
        _cache = Services.GetRequiredService<ICacheService>();
        _settings = Services.GetRequiredService<SharpCacheSettings>();
    }

    public override async ValueTask OnEntryAsync(MethodContext context)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        var key = GetCacheKey(context);
        var value = await _cache.GetAsync(key, context.TaskReturnType);
        if (value != null)
        {
            // check if the cache is out of date
            var isOutOfDate = await IsOutOfDate(context, value);

            if (!isOutOfDate)
            {
                context.ReplaceReturnValue(this, value);
            }
        }
    }

    public override async ValueTask OnSuccessAsync(MethodContext context)
    {
        if (!_settings.Enabled)
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

        if (context.ReturnValue != null)
        {
            var key = GetCacheKey(context);
            await _cache.SetAsync(key, context.ReturnValue, new TimeSpan(0, _minutes, 0));
        }
    }

    public virtual Task<bool> IsOutOfDate(MethodContext context, object value)
    {
        return Task.FromResult(false);
    }


    private string GetCacheKey(MethodContext context)
    {
        var prefixKey = GetPrefixKey(context.Method.Name);
        var argsKey = string.Join("_", context.Arguments.Select(arg => GetCacheKeyByArg(arg)));        

        if (_perInstanceCache && context.Target != null)
        {
            return $"{prefixKey}-{context.Target.GetHashCode()}_{argsKey}";
        }
        else
        {
            return $"{prefixKey}_{argsKey}";
        }        
    }

    private string GetPrefixKey(string name)
    {
        return _settings.Prefix + ":" + name;
    }

    private string GetCacheKeyByArg(object? arg)
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

    public async Task ClearCacheAsync()
    { 
        await _cache.ClearCacheAsync(_settings.Prefix);
    }
}
