namespace BotSharp.Abstraction.Loggers;

/// <summary>
/// Model content generating hook, it can be used for logging, metrics and tracing.
/// </summary>
public interface IContentGeneratingHook
{
    /// <summary>
    /// Before content generating.
    /// </summary>
    /// <returns></returns>
    Task BeforeGenerating(Agent agent, List<RoleDialogModel> conversations) => Task.CompletedTask;

    /// <summary>
    /// Before function is invoked
    /// </summary>
    /// <returns></returns>
    Task BeforeFunctionInvoked(RoleDialogModel message, TokenStatsModel tokenStats) => Task.CompletedTask;

    /// <summary>
    /// After function is invoked
    /// </summary>
    /// <returns></returns>
    Task AfterFunctionInvoked(RoleDialogModel message, TokenStatsModel tokenStats) => Task.CompletedTask;

    /// <summary>
    /// After content generated.
    /// </summary>
    /// <returns></returns>
    Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats) => Task.CompletedTask;
}
