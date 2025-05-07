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

        agent.SecondaryFunctions = agent.SecondaryFunctions.Concat(functions).DistinctBy(x => x.Name).ToList();
        agent.SecondaryInstructions = agent.SecondaryInstructions.Concat(templates).Distinct().ToList();
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
}
