using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP.Hooks;

public class MCPToolAgentHook : AgentHookBase
{
    public override string SelfId => string.Empty;

    public MCPToolAgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override void OnAgentMCPToolLoaded(Agent agent)
    {
        if (agent.Type == AgentType.Routing)
        {
            return;
        }

        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        if (!isConvMode) return;

        agent.SecondaryFunctions ??= [];

        var functions = GetMCPContent(agent).Result;
        foreach (var fn in functions)
        {
            if (!agent.SecondaryFunctions.Any(x => x.Name.Equals(fn.Name, StringComparison.OrdinalIgnoreCase)))
            {
                agent.SecondaryFunctions.Add(fn);
            }
        }
    }

    private async Task<IEnumerable<FunctionDef>> GetMCPContent(Agent agent)
    {
        var functionDefs = new List<FunctionDef>();
        var mcpClientManager = _services.GetRequiredService<MCPClientManager>();
        var mcps = agent.McpTools;
        foreach (var item in mcps)
        {
            var mcpClient =  await mcpClientManager.GetMcpClientAsync(item.ServerId);
            if (mcpClient != null)
            {
                var tools = await mcpClient.ListToolsAsync();
                var toolnames = item.Functions.Select(x => x.Name).ToList();
                foreach (var tool in tools.Where(x => toolnames.Contains(x.Name, StringComparer.OrdinalIgnoreCase)))
                {
                    var funDef = AIFunctionUtilities.MapToFunctionDef(tool);
                    functionDefs.Add(funDef);
                }
            }
        }

        return functionDefs;
    }
}
