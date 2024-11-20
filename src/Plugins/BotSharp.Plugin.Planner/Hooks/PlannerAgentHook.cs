namespace BotSharp.Plugin.Planner.Hooks;

public class PlannerAgentHook : AgentHookBase
{
    private const string PRIMARY_STAGE_FN = "plan_primary_stage";
    private const string SECONDARY_STAGE_FN = "plan_secondary_stage";
    private const string SUMMARY_FN = "plan_summary";

    public override string SelfId => BuiltInAgentId.Planner;

    public PlannerAgentHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
    }

    public override bool OnInstructionLoaded(string template, Dictionary<string, object> dict)
    {
        var knowledgeHooks = _services.GetServices<IKnowledgeHook>();

        // Get global knowledges
        var Knowledges = new List<string>();
        foreach (var hook in knowledgeHooks)
        {
            var k = hook.GetGlobalKnowledges(new RoleDialogModel(AgentRole.User, template)
            {
                CurrentAgentId = BuiltInAgentId.Planner
            }).Result;
            Knowledges.AddRange(k);
        }
        dict["global_knowledges"] = Knowledges;

        return true;
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var utilityLoad = new AgentUtility
        {
            Name = UtilityName.TwoStagePlanner,
            Content = new UtilityContent
            {
                Functions = [
                    new(PRIMARY_STAGE_FN),
                    new(SECONDARY_STAGE_FN),
                    new(SUMMARY_FN)
                ],
                Templates = [
                    new($"{PRIMARY_STAGE_FN}.fn"),
                    new($"{SECONDARY_STAGE_FN}.fn"),
                    new($"{SUMMARY_FN}.fn")
                ]
            }
        };

        base.OnLoadAgentUtility(agent, [utilityLoad]);
        base.OnAgentLoaded(agent);
    }
}
