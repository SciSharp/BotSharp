using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Plugins;
using BotSharp.Core.Mcp.Functions;
using BotSharp.Core.Mcp.Settings;
using BotSharp.MCP.Hooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Configuration;
using System.Linq;
using System.Threading.Tasks;

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
        
        clientManager = new MCPClientManager(settings, NullLoggerFactory.Instance);
        services.AddSingleton(clientManager);

        foreach (var server in settings.McpServerConfigs)
        {
            RegisterFunctionCall(services, server)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
        }
        // Register hooks
        services.AddScoped<IAgentHook, MCPToolAgentHook>();
        services.AddScoped<IConversationHook, MCPResponseHook>();
    }

    private async Task RegisterFunctionCall(IServiceCollection services, McpServerConfig server)
    {
        var client = await clientManager.GetMcpClientAsync(server.Id);
        var tools = await client.ListToolsAsync().ToListAsync(); 

        foreach (var tool in tools)
        {
            services.AddScoped(provider => { return tool; });

            services.AddScoped<IFunctionCallback>(provider =>
            {
                var funcTool = new McpToolFunction(provider, tool, clientManager);
                return funcTool;
            });
        }
    }
}
