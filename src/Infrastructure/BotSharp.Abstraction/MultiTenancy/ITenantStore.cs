using BotSharp.Abstraction.MultiTenancy.Options;

namespace BotSharp.Abstraction.MultiTenancy;

public interface ITenantStore
{
    List<TenantConfiguration> GetTenants();
}