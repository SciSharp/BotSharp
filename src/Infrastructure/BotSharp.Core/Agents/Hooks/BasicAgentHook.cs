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
        var innerUtilities = utilities!.Where(x => !string.IsNullOrEmpty(x.Name) && !x.Disabled).ToList();

        var functionNames = new List<string>();
        var templateNames = new List<string>();

        foreach (var utility in innerUtilities)
        {
            var isVisible = agentService.RenderVisibility(utility.VisibilityExpression, agent.TemplateDict);
            if (!isVisible || utility.Items.IsNullOrEmpty()) continue;

            foreach (var item in utility.Items)
            {
                isVisible = agentService.RenderVisibility(item.VisibilityExpression, agent.TemplateDict);
                if (!isVisible) continue;

                if (item.FunctionName?.StartsWith(UTIL_PREFIX) == true)
                {
                    functionNames.Add(item.FunctionName);
                }

                if (item.TemplateName?.StartsWith(UTIL_PREFIX) == true)
                {
                    templateNames.Add(item.TemplateName);
                }
            }
        }

        return (functionNames.Distinct(), templateNames.Distinct());
    }   
}
