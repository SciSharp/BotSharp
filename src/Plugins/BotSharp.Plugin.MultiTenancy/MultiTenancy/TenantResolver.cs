using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Models;
using BotSharp.Plugin.MultiTenancy.Models;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public class TenantResolver : ITenantResolver
{
    private readonly TenantResolveOptions _option;
    public TenantResolver(IOptions<TenantResolveOptions> option)
    {
        _option = option.Value;
    }

    public async Task<TenantResolveResult> ResolveAsync(TenantResolveContext context)
    {
        foreach (var c in _option.TenantResolvers)
        {
            var result = await c.ResolveAsync(context);
            if (result.Succeeded) return result;
        }

        return new TenantResolveResult();
    }
}