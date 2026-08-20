namespace BotSharp.Abstraction.Routing.Executor;

/// <summary>
/// Decides who executes a given function name. Every function-call path has to go through here,
/// or IFunctionExecutorProvider gets bypassed -- BotSharp.Core.Rules' ToolCallAction used to be
/// exactly such a bypass.
/// </summary>
public interface IFunctionExecutorFactory
{
    IFunctionExecutor? Create(string functionName, Agent agent);
}
