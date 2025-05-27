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

        agent.Utilities ??= [];
        agent.SecondaryFunctions ??= [];
        agent.SecondaryInstructions ??= [];

        var (functions, templates) = GetUtilityContent(agent);

        agent.SecondaryFunctions = agent.SecondaryFunctions.Concat(functions).DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
        var contents = templates.Select(x => x.Content);
        agent.SecondaryInstructions = agent.SecondaryInstructions.Concat(contents).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
     

    private (IEnumerable<FunctionDef>, IEnumerable<AgentTemplate>) GetUtilityContent(Agent agent)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var (functionNames, templateNames) = FilterUtilityContent(agent.Utilities, agent);

        if (agent.MergeUtility)
        {
            var routing = _services.GetRequiredService<IRoutingContext>();
            var entryAgentId = routing.EntryAgentId;
            if (!string.IsNullOrEmpty(entryAgentId))
            {
                var entryAgent = db.GetAgent(entryAgentId, basicsOnly: true);
                var (fns, tps) = FilterUtilityContent(entryAgent?.Utilities, agent);
                functionNames = functionNames.Concat(fns).Distinct().ToList();
                templateNames = templateNames.Concat(tps).Distinct().ToList();
            }
        }

        var ua = db.GetAgent(BuiltInAgentId.UtilityAssistant);
        var functions = ua?.Functions?.Where(x => functionNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase))?.ToList() ?? [];
        var templates = ua?.Templates?.Where(x => templateNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase))?.ToList() ?? [];
        return (functions, templates);
    }

    private (IEnumerable<string>, IEnumerable<string>) FilterUtilityContent(IEnumerable<AgentUtility>? utilities, Agent agent)
    {
        if (utilities.IsNullOrEmpty())
        {
            return ([], []);
        }

        var agentService = _services.GetRequiredService<IAgentService>();
        var innerUtilities = utilities!.Where(x =>
        {
            var isVisible = !string.IsNullOrEmpty(x.Name) && !x.Disabled;
            if (!isVisible)
            {
                return isVisible;
            }

            isVisible = agentService.RenderUtility(agent, x);
            return isVisible;
        }).ToList();

        var functionNames = innerUtilities.SelectMany(x => x.Functions)
                                         .Where(x => !string.IsNullOrEmpty(x.Name) && x.Name.StartsWith(UTIL_PREFIX))
                                         .Select(x => x.Name)
                                         .Distinct().ToList();
        var templateNames = innerUtilities.SelectMany(x => x.Templates)
                                         .Where(x => !string.IsNullOrEmpty(x.Name) && x.Name.StartsWith(UTIL_PREFIX))
                                         .Select(x => x.Name)
                                         .Distinct().ToList();

        return (functionNames, templateNames);
    }   
}
