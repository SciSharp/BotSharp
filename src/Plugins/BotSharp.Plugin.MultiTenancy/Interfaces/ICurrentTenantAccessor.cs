using BotSharp.Plugin.MultiTenancy.Models;

namespace BotSharp.Plugin.MultiTenancy.Interfaces;

public interface ICurrentTenantAccessor
{
    TenantInfoBasic? Current { get; set; }
}