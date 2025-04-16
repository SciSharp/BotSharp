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

    public IEnumerable<McpServerOptionModel> GetServerConfigs()
    {
        var options = new List<McpServerOptionModel>();
        var settings = _services.GetRequiredService<McpSettings>();
        var configs = settings?.McpServerConfigs ?? [];

        foreach (var config in configs)
        {
            var tools = _services.GetServices<IFunctionCallback>()
                                 .Where(x => x.Provider == config.Name)
                                 .Select(x => x.Name);

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
