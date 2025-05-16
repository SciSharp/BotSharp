namespace BotSharp.Abstraction.MCP.Services;

public interface IMcpService
{
    Task<IEnumerable<McpServerOptionModel>> GetServerConfigsAsync() => Task.FromResult<IEnumerable<McpServerOptionModel>>([]);
}
