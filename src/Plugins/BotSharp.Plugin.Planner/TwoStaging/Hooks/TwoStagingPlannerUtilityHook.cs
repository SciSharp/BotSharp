namespace BotSharp.Plugin.Planner.TwoStaging.Hooks;

public class TwoStagingPlannerUtilityHook : IAgentUtilityHook
{
    private const string PRIMARY_STAGE_FN = "plan_primary_stage";
    private const string SECONDARY_STAGE_FN = "plan_secondary_stage";
    private const string SUMMARY_FN = "plan_summary";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Category = "planner",
            Name = UtilityName.TwoStagePlanner,
            Items = [
                new UtilityItem
                {
                    FunctionName = PRIMARY_STAGE_FN,
                    TemplateName = $"{PRIMARY_STAGE_FN}.fn"
                },
                new UtilityItem
                {
                    FunctionName = SECONDARY_STAGE_FN,
                    TemplateName = $"{SECONDARY_STAGE_FN}.fn"
                },
                new UtilityItem
                {
                    FunctionName = SUMMARY_FN,
                    TemplateName = $"{SUMMARY_FN}.fn"
                }
            ]
        };

        utilities.Add(utility);
    }
}
