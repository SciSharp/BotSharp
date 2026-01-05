using BotSharp.Abstraction.MultiTenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MultiTenancy.Controllers;

[ApiController]
public class TenantsController : ControllerBase
{
    private readonly ITenantOptionProvider _tenantOptionProvider;

    public TenantsController(ITenantOptionProvider tenantOptionProvider)
    {
        _tenantOptionProvider = tenantOptionProvider;
    }

    [AllowAnonymous]
    [HttpGet]
    [Route("/tenants/options")]
    public async Task<IActionResult> Options()
    {
        var tenants = await _tenantOptionProvider.GetOptionsAsync();
        var payload = tenants.Select(t => new { tenantId = t.TenantId, name = t.Name }).ToArray();
        return Ok(payload);
    }
}