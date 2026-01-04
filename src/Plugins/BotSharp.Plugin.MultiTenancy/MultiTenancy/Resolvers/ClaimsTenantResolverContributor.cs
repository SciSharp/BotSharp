using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Models;
using BotSharp.Plugin.MultiTenancy.Enums;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy.Resolvers;

public class ClaimsTenantResolveContributor : ITenantResolveContributor
{
    public string Name => "Claims";

    public Task<TenantResolveResult> ResolveAsync(TenantResolveContext context)
    {
        var user = context.HttpContext.User;
        var claim = user.FindFirst(TenantConsts.TenantId);
        if (claim != null && System.Guid.TryParse(claim.Value, out var id))
        {
            return Task.FromResult(new TenantResolveResult { TenantId = id });
        }

        return Task.FromResult(new TenantResolveResult());
    }
}