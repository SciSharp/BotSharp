namespace BotSharp.Abstraction.Routing.Executor;

/// <summary>
/// Lets an external component take over the execution of a function. Returning null means "not
/// mine" and hands off to the next provider, or to the built-in resolution chain. The typical use
/// is swapping a real tool for a fake during a test, or blocking a function by policy.
/// </summary>
public interface IFunctionExecutorProvider
{
    /// <summary>Lower is asked first.</summary>
    int Order => 0;

    IFunctionExecutor? TryResolve(string functionName, Agent agent);
}
