using BotSharp.Abstraction.Functions.Models;

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

    /// <summary>
    /// Rdndering template
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="name"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    Task OnRenderingTemplate(Agent agent, string name, string content) => Task.CompletedTask;

    /// <summary>
    /// Realtime session updated
    /// </summary>
    /// <param name="agent"></param>
    /// <param name="instruction"></param>
    /// <param name="functions"></param>
    /// <returns></returns>
    Task OnSessionUpdated(Agent agent, string instruction, FunctionDef[] functions) => Task.CompletedTask;
}
