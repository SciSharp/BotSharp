using BotSharp.Core.MCP.Functions;
using BotSharp.Core.MCP.Hooks;
using BotSharp.Core.MCP.Managers;
using BotSharp.Core.MCP.Services;
using BotSharp.Core.MCP.Settings;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP;

public static class BotSharpMcpExtensions
{
    /// <summary>
    /// Add mcp 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddBotSharpMCP(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IMcpService, McpService>();
        var settings = config.GetSection("MCP").Get<McpSettings>();
        services.AddScoped(provider => settings);

        if (settings != null && settings.Enabled && !settings.McpServerConfigs.IsNullOrEmpty())
        {
            var clientManager = new McpClientManager(settings);
            services.AddSingleton(clientManager);

            foreach (var server in settings.McpServerConfigs)
            {
                RegisterFunctionCall(services, server, clientManager)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }

            // Register hooks
            services.AddScoped<IAgentHook, McpToolAgentHook>();
        }
        return services;
    }

    private static async Task RegisterFunctionCall(IServiceCollection services, McpServerConfigModel server, McpClientManager clientManager)
    {
        var client = await clientManager.GetMcpClientAsync(server.Id);
        var tools = await client.ListToolsAsync();

        foreach (var tool in tools)
        {
            services.AddScoped(provider => tool);

            services.AddScoped<IFunctionCallback>(provider =>
            {
                var funcTool = new McpToolAdapter(provider, server.Name, tool, clientManager);
                return funcTool;
            });
        }
    }
}