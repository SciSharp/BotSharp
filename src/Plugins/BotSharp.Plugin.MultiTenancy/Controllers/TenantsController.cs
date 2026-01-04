using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

namespace BotSharp.Plugin.MultiTenancy.Controllers;

[ApiController]
public class TenantsController: ControllerBase
{
    private readonly IOptionsMonitor<TenantStoreOptions> _tenantStoreOptions;
    private readonly ITenantFeature _tenantFeature;

    public TenantsController(
        IOptionsMonitor<TenantStoreOptions> tenantStoreOptions,
        ITenantFeature tenantFeature)
    {
        _tenantStoreOptions = tenantStoreOptions;
        _tenantFeature = tenantFeature;
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("/tenants/options")]
    public IActionResult Options()
    {
        if (!_tenantFeature.Enabled)
        {
            return Ok(Array.Empty<object>());
        }

        var tenants = _tenantStoreOptions.CurrentValue.Tenants
            .Select(t => new { tenantId = t.Id, name = t.Name })
            .Distinct()
            .ToArray();

        return Ok(tenants);
    }
}