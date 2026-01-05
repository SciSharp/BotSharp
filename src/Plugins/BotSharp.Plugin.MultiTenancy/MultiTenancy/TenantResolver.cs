using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public class TenantResolver : ITenantResolver
{
    private readonly IEnumerable<ITenantResolveContributor> _contributors;

    public TenantResolver(IEnumerable<ITenantResolveContributor> contributors)
    {
        _contributors = contributors;
    }

    public async Task<TenantResolveResult> ResolveAsync(TenantResolveContext context)
    {
        foreach (var c in _contributors)
        {
            var result = await c.ResolveAsync(context);
            if (result.Succeeded) return result;
        }

        return new TenantResolveResult();
    }
}