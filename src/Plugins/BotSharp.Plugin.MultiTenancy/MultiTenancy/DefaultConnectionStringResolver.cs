using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Options;
using Microsoft.Extensions.Options;
using System.Linq;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public class DefaultConnectionStringResolver : IConnectionStringResolver
{
    private readonly IOptionsMonitor<TenantStoreOptions> _tenantStoreOptions;
    private readonly ICurrentTenant _currentTenant;
    private readonly ITenantStore _tenantStore;

    public DefaultConnectionStringResolver(
        IOptionsMonitor<TenantStoreOptions> tenantStoreOptions,
        ICurrentTenant currentTenant,
        ITenantStore tenantStore)
    {
        _tenantStoreOptions = tenantStoreOptions;
        _currentTenant = currentTenant;
        _tenantStore = tenantStore;
    }

    public string? GetConnectionString(string connectionStringName)
    {
        var options = _tenantStoreOptions.CurrentValue;
        if (!options.Enabled) return null;
        if (!_currentTenant.Id.HasValue) return null;

        // Prefer configured tenants
        var configuredTenants = options.Tenants;
        if (configuredTenants is { Length: > 0 })
        {
            var tenant = configuredTenants.FirstOrDefault(t => t.Id == _currentTenant.Id.Value);
            if (tenant?.ConnectionStrings != null && tenant.ConnectionStrings.TryGetValue(connectionStringName, out var v))
            {
                return v;
            }

            return null;
        }

        // Fallback to store (e.g. DB)
        var storeTenants = _tenantStore.GetTenants();
        var storeTenant = storeTenants.FirstOrDefault(t => t.Id == _currentTenant.Id.Value);
        if (storeTenant?.ConnectionStrings != null && storeTenant.ConnectionStrings.TryGetValue(connectionStringName, out var sv))
        {
            return sv;
        }

        return null;
    }

    public string? GetConnectionString<TContext>()
    {
        var contextType = typeof(TContext);
        var connStringName = ConnectionStringNameAttribute.GetConnStringName(contextType);
        return GetConnectionString(connStringName);
    }
}