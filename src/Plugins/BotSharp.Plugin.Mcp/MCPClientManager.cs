using BotSharp.Plugin.Mcp.Settings;
using McpDotNet.Client;
using Microsoft.Extensions.Logging;
using System;

namespace BotSharp.Plugin.Mcp;

public class MCPClientManager : IDisposable
{
    public ILoggerFactory LoggerFactory { get; }
    public McpClientFactory Factory { get; }


    private readonly MCPSettings mcpSettings;

    public MCPClientManager(MCPSettings settings, ILoggerFactory loggerFactory)
    {
        mcpSettings = settings;
        LoggerFactory = loggerFactory;             

        // Inject the mock transport into the factory
        Factory = new McpClientFactory(
            settings.McpServerConfigs,
            settings.McpClientOptions,
            LoggerFactory
        );
    }

    public void Dispose()
    {
        LoggerFactory?.Dispose();
    }
}
