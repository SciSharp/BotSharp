namespace BotSharp.Abstraction.Planning;

public interface IPlanningHook
{
    Task<string> GetSummaryAdditionalRequirements(string planner)
        => Task.FromResult(string.Empty);

    Task OnPlanningCompleted(string planner, RoleDialogModel msg)
        => Task.CompletedTask;
}
