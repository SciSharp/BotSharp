using BotSharp.Core.MCP.Managers;
using BotSharp.Core.MCP.Settings;
using ModelContextProtocol.Client;

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

    public async Task<IEnumerable<McpServerOptionModel>> GetServerConfigsAsync()
    {
        var clientManager = _services.GetService<McpClientManager>();
        if (clientManager == null) return [];

        var options = new List<McpServerOptionModel>();
        var settings = _services.GetRequiredService<McpSettings>();
        var configs = settings?.McpServerConfigs ?? [];

        foreach (var config in configs)
        {
            var client = await clientManager.GetMcpClientAsync(config.Id);
            if (client == null) continue;

            var tools = await client.ListToolsAsync();
            options.Add(new McpServerOptionModel
            {
                Id = config.Id,
                Name = config.Name,
                Tools = tools.Select(x => x.Name)
            });
        }

        return options;
    }
}
