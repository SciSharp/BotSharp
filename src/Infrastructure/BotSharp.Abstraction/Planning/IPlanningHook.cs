namespace BotSharp.Abstraction.Planning;

public interface IPlanningHook
{
    Task<string> GetSummaryAdditionalRequirements(string planner);
    Task OnPlanningCompleted(string planner, RoleDialogModel msg);
}
