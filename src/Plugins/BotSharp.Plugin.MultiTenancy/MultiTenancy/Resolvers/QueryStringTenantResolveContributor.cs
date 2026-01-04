using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Models;
using BotSharp.Plugin.MultiTenancy.Enums;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy.Resolvers;

public class QueryStringTenantResolveContributor : ITenantResolveContributor
{
    public string Name => "QueryString";

    public Task<TenantResolveResult> ResolveAsync(TenantResolveContext context)
    {
        var request = context.HttpContext.Request;
        if (request.Query.TryGetValue(TenantConsts.TenantId, out var values))
        {
            if (System.Guid.TryParse(values.FirstOrDefault(), out var id))
            {
                return Task.FromResult(new TenantResolveResult { TenantId = id });
            }
        }

        return Task.FromResult(new TenantResolveResult());
    }
}