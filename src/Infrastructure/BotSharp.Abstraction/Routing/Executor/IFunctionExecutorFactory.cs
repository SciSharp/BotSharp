namespace BotSharp.Abstraction.Routing.Executor;

/// <summary>
/// 决定某个函数名由谁执行。所有函数调用路径都必须经过这里，否则 IFunctionExecutorProvider
/// 的接管会出现旁路（历史上 BotSharp.Core.Rules 的 ToolCallAction 就是这样一条旁路）。
/// </summary>
public interface IFunctionExecutorFactory
{
    IFunctionExecutor? Create(string functionName, Agent agent);
}
