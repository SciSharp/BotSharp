using BotSharp.Abstraction.MultiTenancy.Options;

namespace BotSharp.Abstraction.MultiTenancy;

public interface ITenantRepository
{
    List<TenantConfiguration> GetTenants();
}