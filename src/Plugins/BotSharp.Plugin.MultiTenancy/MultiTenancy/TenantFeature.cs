using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Options;
using Microsoft.Extensions.Options;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public class TenantFeature : ITenantFeature
{
    private readonly TenantStoreOptions _tenantStoreOptions;

    public TenantFeature(IOptions<TenantStoreOptions> tenantStoreOptions)
    {
        _tenantStoreOptions = tenantStoreOptions.Value;
    }

    public bool Enabled => _tenantStoreOptions.Enabled;
}