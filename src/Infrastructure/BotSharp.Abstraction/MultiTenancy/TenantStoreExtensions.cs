using BotSharp.Abstraction.MultiTenancy.Options;
using Microsoft.Extensions.Options;

namespace BotSharp.Abstraction.MultiTenancy;

public static class TenantStoreExtensions
{
    public static bool IsEnabled(this IOptionsMonitor<TenantStoreOptions> options)
        => options.CurrentValue.Enabled;

    public static bool HasConfiguredTenants(this IOptionsMonitor<TenantStoreOptions> options)
        => options.CurrentValue.Tenants is { Length: > 0 };

    public static IReadOnlyList<TenantConfiguration> ConfiguredTenants(this IOptionsMonitor<TenantStoreOptions> options)
        => options.CurrentValue.Tenants;
}
