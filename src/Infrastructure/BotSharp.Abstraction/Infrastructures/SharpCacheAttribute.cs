using BotSharp.Abstraction.Infrastructures;
using BotSharp.Abstraction.MultiTenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rougamo;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Rougamo.Context;

namespace BotSharp.Core.Infrastructures;

public class SharpCacheAttribute : AsyncMoAttribute
{
    public static IServiceProvider Services { get; set; } = null!;
    private const string NullKey = "$_is_null";
    private const int MaxRawStringKeyLength = 128;
    private static readonly JsonSerializerOptions KeyJsonOptions = new() { WriteIndented = false };

    /// <summary>
    /// Entries for <c>perInstanceCache</c>, attached to the instance they belong to. Such an entry can only
    /// ever be hit through that instance, so it is collected together with it. In the shared cache it stayed
    /// for the full expiry after a scoped service's scope had ended, one dead copy per scope.
    /// </summary>
    private static readonly ConditionalWeakTable<object, ConcurrentDictionary<string, InstanceCacheEntry>> InstanceCaches = new();

    private sealed record InstanceCacheEntry(object Value, DateTime ExpiresAt);

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
        if (key == null)
        {
            return;
        }

        var value = IsInstanceScoped(context)
            ? GetInstanceValue(context.Target!, key)
            : await _cache.GetAsync(key, context.TaskReturnType);
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
            if (key == null)
            {
                return;
            }

            var expiry = new TimeSpan(0, _minutes, 0);
            if (IsInstanceScoped(context))
            {
                InstanceCaches.GetOrCreateValue(context.Target!)[key] = new InstanceCacheEntry(context.ReturnValue, DateTime.UtcNow + expiry);
            }
            else
            {
                await _cache.SetAsync(key, context.ReturnValue, expiry);
            }
        }
    }

    private bool IsInstanceScoped(MethodContext context)
        => _perInstanceCache && context.Target != null;

    private static object? GetInstanceValue(object target, string key)
    {
        if (InstanceCaches.TryGetValue(target, out var entries) && entries.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                return entry.Value;
            }
            entries.TryRemove(key, out _);
        }
        return null;
    }

    public virtual Task<bool> IsOutOfDate(MethodContext context, object value)
    {
        return Task.FromResult(false);
    }


    /// <summary>
    /// Returns null when an argument cannot be turned into a stable key; the call then bypasses the cache
    /// rather than risk a key that never hits or that collides with another caller's entry.
    /// </summary>
    private string? GetCacheKey(MethodContext context)
    {
        var argKeys = new List<string>(context.Arguments.Length);
        foreach (var arg in context.Arguments)
        {
            if (!TryGetCacheKeyByArg(arg, out var argKey))
            {
                Services.GetService<ILogger<SharpCacheAttribute>>()?.LogDebug(
                    "SharpCache bypassed for {Method}: argument of type {ArgType} has no stable cache key.",
                    context.Method.Name, arg?.GetType().FullName);
                return null;
            }
            argKeys.Add(argKey);
        }

        // Instance-scoped entries live on the instance itself, so the key needs no instance part.
        return $"{GetPrefixKey(context.Method.Name)}_{string.Join("_", argKeys)}";
    }

    private string GetPrefixKey(string name)
    {
        var tenantId = GetTenantId();
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            return $"{_settings.Prefix}:{tenantId}:{name}";
        }

        return _settings.Prefix + ":" + name;
    }

    private string? GetTenantId()
    {
        try
        {
            var tenant = Services.GetService<ICurrentTenant>();
            return tenant?.TenantId;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds the key from the argument's content. Reference-type arguments used to be keyed by
    /// <c>GetHashCode()</c>, i.e. object identity: a freshly built filter or list never hit the cache,
    /// so every call parked another copy of the result, and a 32-bit hash collision could hand one
    /// caller another caller's entry.
    /// </summary>
    private static bool TryGetCacheKeyByArg(object? arg, out string key)
    {
        switch (arg)
        {
            case null:
                key = NullKey;
                return true;
            case ICacheKey withCacheKey:
                key = withCacheKey.GetCacheKey();
                return true;
            case string text:
                // Length-prefixed so "a_b" + "c" and "a" + "b_c" cannot join into the same key.
                key = text.Length <= MaxRawStringKeyLength ? $"s{text.Length}:{text}" : $"h:{Sha256(text)}";
                return true;
            case DateTime or DateTimeOffset:
                key = ((IFormattable)arg).ToString("o", CultureInfo.InvariantCulture);
                return true;
        }

        var type = arg.GetType();
        if (type.IsPrimitive || type.IsEnum || arg is decimal or Guid or TimeSpan)
        {
            key = Convert.ToString(arg, CultureInfo.InvariantCulture) ?? string.Empty;
            return true;
        }

        try
        {
            var json = JsonSerializer.Serialize(arg, type, KeyJsonOptions);
            key = $"j:{Sha256(type.FullName + json)}";
            return true;
        }
        catch (Exception ex) when (ex is NotSupportedException or JsonException or InvalidOperationException)
        {
            key = string.Empty;
            return false;
        }
    }

    private static string Sha256(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    public async Task ClearCacheAsync()
    {
        var tenantId = GetTenantId();
        var prefix = string.IsNullOrWhiteSpace(tenantId) ? _settings.Prefix : $"{_settings.Prefix}:{tenantId}";
        await _cache.ClearCacheAsync(prefix);

        // Instance-scoped entries were cleared along with the shared cache before they moved out of it.
        foreach (var (_, entries) in InstanceCaches)
        {
            entries.Clear();
        }
    }

}
