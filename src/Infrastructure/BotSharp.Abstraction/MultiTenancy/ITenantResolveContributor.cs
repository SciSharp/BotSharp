using BotSharp.Abstraction.MultiTenancy.Models;

namespace BotSharp.Abstraction.MultiTenancy;

public interface ITenantResolveContributor
{
    string Name { get; }

    Task<TenantResolveResult> ResolveAsync(TenantResolveContext context);
}