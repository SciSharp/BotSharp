using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Models;
using BotSharp.Abstraction.MultiTenancy.Options;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy.Providers;

public class ConfigTenantOptionProvider : ITenantOptionProvider
{
    private readonly IOptionsMonitor<TenantStoreOptions> _tenantStoreOptions;
    private readonly ITenantFeature _feature;

    public ConfigTenantOptionProvider(IOptionsMonitor<TenantStoreOptions> tenantStoreOptions, ITenantFeature feature)
    {
        _tenantStoreOptions = tenantStoreOptions;
        _feature = feature;
    }

    public Task<IReadOnlyList<TenantOption>> GetOptionsAsync()
    {
        if (!_feature.Enabled)
        {
            return Task.FromResult<IReadOnlyList<TenantOption>>(Array.Empty<TenantOption>());
        }

        var tenants = _tenantStoreOptions.CurrentValue.Tenants
            .Select(t => new TenantOption(t.Id, t.Name))
            .Distinct()
            .ToArray();

        return Task.FromResult<IReadOnlyList<TenantOption>>(tenants);
    }
}