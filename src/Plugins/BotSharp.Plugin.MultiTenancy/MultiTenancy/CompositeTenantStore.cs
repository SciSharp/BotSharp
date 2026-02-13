using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Options;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public class CompositeTenantStore : ITenantStore
{
    private readonly IOptionsMonitor<TenantStoreOptions> _options;
    private readonly IEnumerable<ITenantStore> _stores;

    public CompositeTenantStore(IOptionsMonitor<TenantStoreOptions> options, IEnumerable<ITenantStore> stores)
    {
        _options = options;
        _stores = stores;
    }

    public List<TenantConfiguration> GetTenants()
    {
        // If configuration has tenants, prefer it.
        var configured = _options.CurrentValue.Tenants;
        if (configured is { Length: > 0 })
        {
            return configured.ToList();
        }

        // Otherwise, try other stores in order.
        foreach (var s in _stores)
        {
            if (s is ConfigTenantStore) continue;
            var tenants = s.GetTenants();
            if (tenants.Count > 0) return tenants;
        }

        return new List<TenantConfiguration>();
    }
}