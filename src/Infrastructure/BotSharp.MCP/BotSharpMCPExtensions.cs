using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Functions;
using BotSharp.Core.Mcp.Functions;
using BotSharp.Core.Mcp.Settings;
using BotSharp.Core.Mcp;
using BotSharp.MCP.Hooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModelContextProtocol.Client;

namespace BotSharp.MCP;

public static class BotSharpMCPExtensions
{
    /// <summary>
    /// Add mcp 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddBotSharpMCP(this IServiceCollection services,
        IConfiguration config)
    {
        var settings = config.GetSection("MCPSettings").Get<MCPSettings>();
        services.AddScoped<MCPSettings>(provider => { return settings; });
        if (settings != null)
        {

            var clientManager = new MCPClientManager(settings);
            services.AddSingleton(clientManager);

            foreach (var server in settings.McpServerConfigs)
            {
                RegisterFunctionCall(services, server, clientManager)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
            // Register hooks
            services.AddScoped<IAgentHook, MCPToolAgentHook>();
        }
        return services;
    }

    private static async Task RegisterFunctionCall(IServiceCollection services, McpServerConfig server, MCPClientManager clientManager)
    {
        var client = await clientManager.GetMcpClientAsync(server.Id);
        var tools = await client.ListToolsAsync();

        foreach (var tool in tools)
        {
            services.AddScoped(provider => { return tool; });

            services.AddScoped<IFunctionCallback>(provider =>
            {
                var funcTool = new McpToolAdapter(provider, tool, clientManager);
                return funcTool;
            });
        }
    }
}