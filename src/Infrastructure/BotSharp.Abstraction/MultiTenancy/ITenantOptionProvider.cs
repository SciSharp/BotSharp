using BotSharp.Abstraction.MultiTenancy.Models;

namespace BotSharp.Abstraction.MultiTenancy;

public interface ITenantOptionProvider
{
    Task<IReadOnlyList<TenantOption>> GetOptionsAsync();
}