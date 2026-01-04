using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MultiTenancy.MultiTenancy;

public class MultiTenancyMiddleware : IMiddleware
{
    private readonly ITenantResolver _resolver;
    private readonly ICurrentTenant _currentTenant;

    public MultiTenancyMiddleware(ITenantResolver resolver, ICurrentTenant currentTenant)
    {
        _resolver = resolver;
        _currentTenant = currentTenant;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var resolveContext = new TenantResolveContext { HttpContext = context };
        var resolveResult = await _resolver.ResolveAsync(resolveContext);
        if (resolveResult.TenantId.HasValue)
        {
            using (_currentTenant.Change(resolveResult.TenantId, resolveResult.Name))
            {
                await next(context);
            }
        }
        else
        {
            await next(context);
        }
    }
}