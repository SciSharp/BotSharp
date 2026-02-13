using BotSharp.Plugin.MultiTenancy.Models;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public interface ICurrentTenantAccessor
{
    TenantInfoBasic? Current { get; set; }
}