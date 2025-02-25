using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Plugins;
using BotSharp.Core.Mcp.Functions;
using BotSharp.Core.Mcp.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BotSharp.Core.Mcp;

public class McpPlugin : IBotSharpPlugin
{
    public string Id => "5d779611-0012-46cb-a754-4ca4770e88ac";
    public string Name => "MCP Plugin";
    public string Description => "Integrated MCP tools";

    private MCPClientManager clientManager;

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = config.GetSection("MCPSettings").Get<MCPSettings>();
        services.AddScoped<MCPSettings>(provider => { return settings; });
        services.AddSingleton<MCPClientManager>(provider =>
        {
            var loggerFactory = provider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            return new MCPClientManager(settings, loggerFactory);
        });

        foreach (var server in settings.McpServerConfigs)
        {
            var client = clientManager.Factory.GetClientAsync(server.Id).Result;
            var tools = client.ListToolsAsync().Result;

            foreach (var tool in tools.Tools)
            {
                services.AddScoped<IFunctionCallback>(provider =>
                {
                    var func = new McpToolFunction(tool, client);
                    return func;
                });
            }
        }
    }
 }
