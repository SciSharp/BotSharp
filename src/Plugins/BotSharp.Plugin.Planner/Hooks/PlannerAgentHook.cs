namespace BotSharp.Plugin.Planner.Hooks;

public class PlannerAgentHook : AgentHookBase
{
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
}
