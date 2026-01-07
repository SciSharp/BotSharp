using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Models;
using BotSharp.Plugin.MultiTenancy.Enums;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy.Resolvers;

public class HeaderTenantResolveContributor : ITenantResolveContributor
{
    public string Name => "Header";

    public Task<TenantResolveResult> ResolveAsync(TenantResolveContext context)
    {
        var request = context.HttpContext.Request;
        if (request.Headers.TryGetValue(TenantConsts.DefaultTenantKey, out var values))
        {
            if (Guid.TryParse(values.FirstOrDefault(), out var id))
            {
                return Task.FromResult(new TenantResolveResult { TenantId = id });
            }
        }

        return Task.FromResult(new TenantResolveResult());
    }
}