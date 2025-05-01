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

    public override void OnAgentMcpToolLoaded(Agent agent)
    {
        if (agent.Type == AgentType.Routing)
        {
            return;
        }

        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        if (!isConvMode) return;

        agent.SecondaryFunctions ??= [];

        var functions = GetMcpContent(agent).Result;
        foreach (var fn in functions)
        {
            if (!agent.SecondaryFunctions.Any(x => x.Name.Equals(fn.Name, StringComparison.OrdinalIgnoreCase)))
            {
                agent.SecondaryFunctions.Add(fn);
            }
        }
    }

    private async Task<IEnumerable<FunctionDef>> GetMcpContent(Agent agent)
    {
        var functionDefs = new List<FunctionDef>();

        var settings = _services.GetRequiredService<McpSettings>();
        if (settings?.Enabled != true)
        {
            return functionDefs;
        }
        
        var mcpClientManager = _services.GetRequiredService<McpClientManager>();
        var mcps = agent.McpTools.Where(x => !x.Disabled);
        foreach (var item in mcps)
        {
            var mcpClient =  await mcpClientManager.GetMcpClientAsync(item.ServerId);
            if (mcpClient != null)
            {
                var tools = await mcpClient.ListToolsAsync();
                var toolnames = item.Functions.Select(x => x.Name).ToList();
                foreach (var tool in tools.Where(x => toolnames.Contains(x.Name, StringComparer.OrdinalIgnoreCase)))
                {
                    var funDef = AiFunctionHelper.MapToFunctionDef(tool);
                    functionDefs.Add(funDef);
                }
            }
        }

        return functionDefs;
    }
}
