namespace BotSharp.Abstraction.MCP.Services;

public interface IMcpService
{
    IEnumerable<McpServerOptionModel> GetServerConfigs() => [];
}
