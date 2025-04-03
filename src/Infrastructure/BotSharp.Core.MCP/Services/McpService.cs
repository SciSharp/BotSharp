using BotSharp.Core.MCP.Settings;
using Microsoft.Extensions.Logging;

namespace BotSharp.Core.MCP.Services;

public class McpService : IMcpService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<McpService> _logger;

    public McpService(
        IServiceProvider services,
        ILogger<McpService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public IEnumerable<McpServerConfigModel> GetServerConfigs()
    {
        var settings = _services.GetRequiredService<McpSettings>();
        var configs = settings?.McpServerConfigs ?? [];
        return configs.Select(x => new McpServerConfigModel
        {
            Id = x.Id,
            Name = x.Name,
            TransportType = x.TransportType,
            TransportOptions = x.TransportOptions,
            Arguments = x.Arguments,
            Location = x.Location
        });
    }
}
