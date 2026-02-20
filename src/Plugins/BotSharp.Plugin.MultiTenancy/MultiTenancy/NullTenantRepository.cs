using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Options;
using System.Collections.Generic;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public class NullTenantRepository : ITenantRepository
{
    public List<TenantConfiguration> GetTenants()
    {
        return new List<TenantConfiguration>();
    }
}