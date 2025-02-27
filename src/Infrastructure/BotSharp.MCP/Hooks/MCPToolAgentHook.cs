using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Settings;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Core.Mcp;
using BotSharp.Core.MCP;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BotSharp.MCP.Hooks;

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
            return;
        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        if (!isConvMode) return;

        agent.SecondaryFunctions ??= [];

        var functions = GetMCPContent(agent);

        foreach (var fn in functions)
        {
            if (!agent.SecondaryFunctions.Any(x => x.Name.Equals(fn.Name, StringComparison.OrdinalIgnoreCase)))
            {
                agent.SecondaryFunctions.Add(fn);
            }
        }
    }

    private IEnumerable<FunctionDef> GetMCPContent(Agent agent)
    {
        List<FunctionDef> functionDefs = new List<FunctionDef>();
        var mcpClientManager = _services.GetRequiredService<MCPClientManager>();
        var mcps = agent.McpTools;
        foreach (var item in mcps)
        {
            var ua = mcpClientManager.Factory.GetClientAsync(item.ServerId).Result;
            if (ua != null)
            {
                var tools = ua.ListToolsAsync().Result;
                var funcnames = item.Functions.Select(x => x.Name).ToList();
                foreach (var tool in tools.Tools.Where(x => funcnames.Contains(x.Name, StringComparer.OrdinalIgnoreCase)))
                {
                    var funDef = AIFunctionUtilities.MapToFunctionDef(tool);
                    functionDefs.Add(funDef);
                }
            }
        }

        return functionDefs;
    }
}
