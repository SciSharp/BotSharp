using BotSharp.Abstraction.MCP.Models;
using BotSharp.Abstraction.MCP.Services;

namespace BotSharp.OpenAPI.Controllers;

public class McpController : ControllerBase
{
    private readonly IServiceProvider _services;

    public McpController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpGet("/mcp/server-configs")]
    public IEnumerable<McpServerOptionModel> GetMcpServerConfigs()
    {
        var mcp = _services.GetRequiredService<IMcpService>();
        return mcp.GetServerConfigs();
    }
}
