namespace BotSharp.Plugin.Planner.Hooks;

public class PlannerUtilityHook : IAgentUtilityHook
{
    private const string PRIMARY_STAGE_FN = "plan_primary_stage";
    private const string SECONDARY_STAGE_FN = "plan_secondary_stage";
    private const string SUMMARY_FN = "plan_summary";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Name = UtilityName.TwoStagePlanner,
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
        };

        utilities.Add(utility);
    }
}
