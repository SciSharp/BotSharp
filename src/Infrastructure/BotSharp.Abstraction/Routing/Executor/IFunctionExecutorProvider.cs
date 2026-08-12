namespace BotSharp.Abstraction.Routing.Executor;

/// <summary>
/// 让外部接管某个函数的执行。返回 null 表示"我不接管"，交给下一个 provider 或内置解析链。
/// 典型用途是测试期把真实工具替换成假实现，或按策略阻断某个函数。
/// </summary>
public interface IFunctionExecutorProvider
{
    /// <summary>小的先问。</summary>
    int Order => 0;

    IFunctionExecutor? TryResolve(string functionName, Agent agent);
}
