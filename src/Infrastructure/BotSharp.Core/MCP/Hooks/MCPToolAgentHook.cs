using BotSharp.Core.MCP.Helpers;
using BotSharp.Core.MCP.Managers;
using BotSharp.Core.MCP.Settings;
using ModelContextProtocol.Client;

namespace BotSharp.Core.MCP.Hooks;

public class McpToolAgentHook : AgentHookBase
{
    public override string SelfId => string.Empty;

    public McpToolAgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override async Task OnAgentMcpToolLoaded(Agent agent)
    {
        if (agent.Type == AgentType.Routing)
        {
            return;
        }

        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        if (!isConvMode) return;

        agent.SecondaryFunctions ??= [];

        var functions = await GetMcpContent(agent);
        agent.SecondaryFunctions = agent.SecondaryFunctions.Concat(functions).DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<IEnumerable<FunctionDef>> GetMcpContent(Agent agent)
    {
        var functionDefs = new List<FunctionDef>();

        var settings = _services.GetRequiredService<McpSettings>();
        if (settings?.Enabled != true)
        {
            return functionDefs;
        }
        
        var mcpClientManager = _services.GetService<McpClientManager>();
        if (mcpClientManager == null)
        {
            return functionDefs;
        }

        var mcps = agent.McpTools?.Where(x => !x.Disabled) ?? [];
        foreach (var item in mcps)
        {
            // Cached per server for a short window: this runs on every agent load, and listing
            // tools costs a session of its own against the server.
            var tools = await mcpClientManager.GetToolDefinitionsAsync(item.ServerId);
            if (tools.Count == 0) continue;

            var toolNames = item.Functions.Select(x => x.Name).ToList();
            functionDefs.AddRange(tools.Where(x => toolNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase)));
        }

        return functionDefs;
    }
}
