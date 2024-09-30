using BotSharp.Plugin.SqlDriver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.SqlDriver.Controllers;

[Authorize]
[ApiController]
public class SqlDriverController : ControllerBase
{
    private readonly IServiceProvider _services;

    public SqlDriverController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpPost("/knowledge/database/import")]
    public async Task<bool> ImportDbKnowledge(ImportDbKnowledgeRequest request)
    {
        var dbKnowledge = _services.GetRequiredService<DbKnowledgeService>();
        return await dbKnowledge.Import(request);
    }
}
