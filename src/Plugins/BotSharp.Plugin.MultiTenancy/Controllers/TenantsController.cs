using BotSharp.Abstraction.MultiTenancy;
using BotSharp.Abstraction.MultiTenancy.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Linq;

namespace BotSharp.Plugin.MultiTenancy.Controllers;

[ApiController]
public class TenantsController : ControllerBase
{
    private readonly ITenantStore _tenantStore;
    private readonly IOptionsMonitor<TenantStoreOptions> _options;

    public TenantsController(ITenantStore tenantStore, IOptionsMonitor<TenantStoreOptions> options)
    {
        _tenantStore = tenantStore;
        _options = options;
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("/tenants/options")]
    public IActionResult Options()
    {
        if (!_options.CurrentValue.Enabled)
        {
            return Ok(System.Array.Empty<object>());
        }

        var tenants = _tenantStore.GetTenants();
        var payload = tenants
            .Select(t => new { tenantId = t.Id, name = t.Name })
            .Distinct()
            .ToArray();

        return Ok(payload);
    }
}