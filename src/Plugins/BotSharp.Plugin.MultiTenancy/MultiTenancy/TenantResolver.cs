using BotSharp.Abstraction.MultiTenancy.Models;
using BotSharp.Plugin.MultiTenancy.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public class TenantResolver : ITenantResolver
{
    private readonly TenantResolveOptions _tenantResolveOptions;
    private readonly IServiceProvider _serviceProvider;

    public TenantResolver(IOptions<TenantResolveOptions> tenantResolveOptions, IServiceProvider serviceProvider)
    {
        _tenantResolveOptions = tenantResolveOptions.Value;
        _serviceProvider = serviceProvider;
    }

    public async Task<TenantResolveResult> ResolveAsync(TenantResolveContext context)
    {
        if (_tenantResolveOptions.TenantResolvers.Any())
        {
            using (var serviceScope = _serviceProvider.CreateScope())
            {
                foreach (var c in _tenantResolveOptions.TenantResolvers)
                {
                    var result = await c.ResolveAsync(context);
                    if (result.Succeeded) return result;
                }
            }
        }

        return new TenantResolveResult();
    }
}