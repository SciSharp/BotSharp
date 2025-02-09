namespace BotSharp.Plugin.Planner.SqlGeneration.Hooks;

public class SqlPlannerAgentHook : AgentHookBase
{
    public override string SelfId => PlannerAgentId.SqlPlanner;

    public SqlPlannerAgentHook(IServiceProvider services, AgentSettings settings)
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
                CurrentAgentId = PlannerAgentId.SqlPlanner
            }).Result;
            Knowledges.AddRange(k);
        }
        dict["global_knowledges"] = Knowledges;

        return true;
    }
}
