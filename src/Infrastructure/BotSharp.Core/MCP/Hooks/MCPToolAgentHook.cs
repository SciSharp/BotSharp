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
            var mcpClient =  await mcpClientManager.GetMcpClientAsync(item.ServerId);
            if (mcpClient == null) continue;

            var tools = await mcpClient.ListToolsAsync();
            var toolNames = item.Functions.Select(x => x.Name).ToList();
            var targetTools = tools.Where(x => toolNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase));
            foreach (var tool in targetTools)
            {
                var funDef = AiFunctionHelper.MapToFunctionDef(tool);
                if (funDef != null)
                {
                    functionDefs.Add(funDef);
                }
            }
        }

        return functionDefs;
    }
}
