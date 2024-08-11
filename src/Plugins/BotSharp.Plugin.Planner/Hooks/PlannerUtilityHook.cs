namespace BotSharp.Plugin.Planner.Hooks;

public class PlannerUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(UtilityName.TwoStagePlanner);
    }
}
