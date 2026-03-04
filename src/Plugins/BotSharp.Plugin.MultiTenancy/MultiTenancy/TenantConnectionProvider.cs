using BotSharp.Abstraction.MultiTenancy;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public class TenantConnectionProvider : ITenantConnectionProvider
{
    private readonly IConnectionStringResolver _resolver;
    private readonly IConfiguration _configuration;

    public TenantConnectionProvider(IConnectionStringResolver resolver, IConfiguration configuration)
    {
        _resolver = resolver;
        _configuration = configuration;
    }

    public string GetConnectionString(string name)
    {
        // Prefer tenant-level connection strings
        var cs = _resolver.GetConnectionString(name);
        if (!string.IsNullOrWhiteSpace(cs)) return cs;

        // Fallback to app-level connection strings
        var fallback = _configuration.GetConnectionString(name);
        return fallback ?? string.Empty;
    }

    public string GetDefaultConnectionString()
    {
        return GetConnectionString("Default");
    }
}