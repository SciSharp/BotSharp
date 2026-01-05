using BotSharp.Abstraction.MultiTenancy.Models;

namespace BotSharp.Abstraction.MultiTenancy;

public interface ITenantResolver
{
    Task<TenantResolveResult> ResolveAsync(TenantResolveContext context);
}
