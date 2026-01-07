using Microsoft.AspNetCore.Http;

namespace BotSharp.Abstraction.MultiTenancy.Models;

public class TenantResolveContext
{
    public HttpContext HttpContext { get; set; } = default!;
}