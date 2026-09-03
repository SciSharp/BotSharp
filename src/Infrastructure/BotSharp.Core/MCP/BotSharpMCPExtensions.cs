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
        services.AddScoped<IMcpService, McpService>();

        if (settings != null && settings.Enabled && !settings.McpServerConfigs.IsNullOrEmpty())
        {
            // McpClientManager opens every connection over a client from this factory, so that
            // connections to one server share a pooled handler instead of each building its own.
            // Idempotent, and a host that already called it is unaffected.
            services.AddHttpClient();

            services.AddScoped<McpClientManager>();
            services.AddScoped<IAgentHook, McpToolAgentHook>();
        }
        return services;
    }

}