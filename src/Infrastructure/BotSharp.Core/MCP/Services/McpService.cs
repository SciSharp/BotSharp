using BotSharp.Core.MCP.Managers;
using BotSharp.Core.MCP.Settings;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP.Services;

public class McpService : IMcpService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<McpService> _logger;
    private readonly McpClientManager _mcpClientManager;

    public McpService(
        IServiceProvider services,
        ILogger<McpService> logger, 
        McpClientManager mcpClient)
    {
        _services = services;
        _logger = logger;
        _mcpClientManager = mcpClient;
    }

    public IEnumerable<McpServerOptionModel> GetServerConfigs()
    {
        var options = new List<McpServerOptionModel>();
        var settings = _services.GetRequiredService<McpSettings>();
        var configs = settings?.McpServerConfigs ?? [];

        foreach (var config in configs)
        {
            var tools = _mcpClientManager.GetMcpClientAsync(config.Id)
                .Result.ListToolsAsync()
                .Result.Select(x=> x.Name);

            options.Add(new McpServerOptionModel
            {
                Id = config.Id,
                Name = config.Name,
                Tools = tools
            });
        }

        return options;
    }
}
