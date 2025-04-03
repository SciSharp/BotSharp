namespace BotSharp.Abstraction.MCP.Services;

public interface IMcpService
{
    IEnumerable<McpServerConfigModel> GetServerConfigs() => [];
}
