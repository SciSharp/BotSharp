using BotSharp.Core.Mcp;
using BotSharp.Core.MCP;

namespace BotSharp.Core.Agents.Hooks;

public class BasicAgentHook : AgentHookBase
{
    public override string SelfId => string.Empty;

    private const string UTIL_PREFIX = "util-";

    public BasicAgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override void OnAgentUtilityLoaded(Agent agent)
    {
        if (agent.Type == AgentType.Routing) return;

        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        if (!isConvMode) return;

        agent.SecondaryFunctions ??= [];
        agent.SecondaryInstructions ??= [];
        agent.Utilities ??= [];

        var (functions, templates) = GetUtilityContent(agent);

        foreach (var fn in functions)
        {
            if (!agent.SecondaryFunctions.Any(x => x.Name.Equals(fn.Name, StringComparison.OrdinalIgnoreCase)))
            {
                agent.SecondaryFunctions.Add(fn);
            }
        }

        foreach (var prompt in templates)
        {
            agent.SecondaryInstructions.Add(prompt);
        }
    }

    public override void OnAgentCPLoaded(Agent agent)
    {
        if (agent.Type == AgentType.Routing) 
            return;

        var conv = _services.GetRequiredService<IConversationService>();
        var isConvMode = conv.IsConversationMode();
        if (!isConvMode) return;

        agent.SecondaryFunctions ??= [];
        agent.SecondaryInstructions ??= [];
        agent.Acps ??= [];

        var (functions, templates) = GetACPContent(agent);

        foreach (var fn in functions)
        {
            if (!agent.SecondaryFunctions.Any(x => x.Name.Equals(fn.Name, StringComparison.OrdinalIgnoreCase)))
            {
                agent.SecondaryFunctions.Add(fn);
            }
        }

        foreach (var prompt in templates)
        {
            agent.SecondaryInstructions.Add(prompt);
        }
    }
 

    private (IEnumerable<FunctionDef>, IEnumerable<string>) GetUtilityContent(Agent agent)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var (functionNames, templateNames) = GetUniqueContent(agent.Utilities);

        if (agent.MergeUtility)
        {
            var routing = _services.GetRequiredService<IRoutingContext>();
            var entryAgentId = routing.EntryAgentId;
            if (!string.IsNullOrEmpty(entryAgentId))
            {
                var entryAgent = db.GetAgent(entryAgentId, basicsOnly: true);
                var (fns, tps) = GetUniqueContent(entryAgent?.Utilities);
                functionNames = functionNames.Concat(fns).Distinct().ToList();
                templateNames = templateNames.Concat(tps).Distinct().ToList();
            }
        }

        var ua = db.GetAgent(BuiltInAgentId.UtilityAssistant);
        var functions = ua?.Functions?.Where(x => functionNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase))?.ToList() ?? [];
        var templates = ua?.Templates?.Where(x => templateNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase))?.Select(x => x.Content)?.ToList() ?? [];
        return (functions, templates);
    }

    private (IEnumerable<string>, IEnumerable<string>) GetUniqueContent(IEnumerable<AgentUtility>? utilities)
    {
        if (utilities.IsNullOrEmpty())
        {
            return ([], []);
        }

        utilities = utilities?.Where(x => !string.IsNullOrEmpty(x.Name) && !x.Disabled)?.ToList() ?? [];
        var functionNames = utilities.SelectMany(x => x.Functions)
                                     .Where(x => !string.IsNullOrEmpty(x.Name) && x.Name.StartsWith(UTIL_PREFIX))
                                     .Select(x => x.Name)
                                     .Distinct().ToList();
        var templateNames = utilities.SelectMany(x => x.Templates)
                                     .Where(x => !string.IsNullOrEmpty(x.Name) && x.Name.StartsWith(UTIL_PREFIX))
                                     .Select(x => x.Name)
                                     .Distinct().ToList();

        return (functionNames, templateNames);
    }

    private (IEnumerable<FunctionDef>, IEnumerable<string>) GetACPContent(Agent agent)
    {
        List<FunctionDef> functionDefs = new List<FunctionDef>();
        IEnumerable<string> templates = new List<string>();
        var mcpClientManager = _services.GetRequiredService<MCPClientManager>();
        var mcps = agent.Acps;
        foreach (var item in mcps)
        {
            var ua = mcpClientManager.Factory.GetClientAsync(item.ServerId).Result;
            if (ua != null)
            {
               var tools = ua.ListToolsAsync().Result;
               var funcnames = item.Functions.Select(x => x.Name).ToList();
                foreach (var tool in tools.Tools.Where(x=> funcnames.Contains(x.Name,StringComparer.OrdinalIgnoreCase)))
                {
                    var funDef = AIFunctionUtilities.MapToFunctionDef(tool);
                    functionDefs.Add(funDef);
                }
            }
        }
        
         return (functionDefs, templates);
    }

}
