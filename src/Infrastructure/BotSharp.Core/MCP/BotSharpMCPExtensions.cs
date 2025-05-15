using BotSharp.Core.MCP.Hooks;
using BotSharp.Core.MCP.Managers;
using BotSharp.Core.MCP.Services;
using BotSharp.Core.MCP.Settings;
using Microsoft.Extensions.Configuration;

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
        var settings = config.GetSection("MCP").Get<McpSettings>();
        services.AddScoped(provider => settings);

        if (settings != null && settings.Enabled && !settings.McpServerConfigs.IsNullOrEmpty())
        {
            services.AddScoped<IMcpService, McpService>();

            var clientManager = new McpClientManager(settings);
            services.AddScoped(provider => clientManager);

            // Register hooks
            services.AddScoped<IAgentHook, McpToolAgentHook>();
        }
        return services;
    }

}