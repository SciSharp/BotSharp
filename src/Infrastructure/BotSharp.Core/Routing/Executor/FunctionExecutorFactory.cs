using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing.Executor;

namespace BotSharp.Core.Routing.Executor;

public class FunctionExecutorFactory : IFunctionExecutorFactory
{
    private readonly IServiceProvider _services;

    public FunctionExecutorFactory(IServiceProvider services)
    {
        _services = services;
    }

    public IFunctionExecutor? Create(string functionName, Agent agent)
    {
        // 这是全仓唯一决定"某个函数名由谁执行"的地方（见接口自身的文档注释），因此不能靠每个
        // 调用方自己先做好校验——RoutingService.InvokeFunction 就没有任何保护，直接把 name 传到
        // 这里。一个 null/空白的函数名如果真的传给下面注册的 IFunctionExecutorProvider，某些实现
        // （比如按函数名做 Dictionary 查找的 mock/阻断 provider——ToolCallActionTests 的
        // NullIntolerantProvider 已经证明这种形状是真实存在的）会直接抛 ArgumentNullException，
        // 而不是像"找不到函数"那样优雅地返回 null。在这里挡一次，让每个调用方都不必自己重复这
        // 同一个判断。
        if (string.IsNullOrWhiteSpace(functionName))
        {
            return null;
        }

        // 先让外部 provider 有机会接管；顺序稳定（Order 升序），不依赖 DI 注册顺序。
        var providers = _services.GetServices<IFunctionExecutorProvider>().OrderBy(x => x.Order);
        foreach (var provider in providers)
        {
            var claimed = provider.TryResolve(functionName, agent);
            if (claimed != null)
            {
                return claimed;
            }
        }

        // 以下三段为改动前的原样逻辑，顺序与语义均未变更。
        var functionCall = _services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name == functionName);
        if (functionCall != null)
        {
            return new FunctionCallbackExecutor(functionCall);
        }

        var functions = (agent?.Functions ?? []).Concat(agent?.SecondaryFunctions ?? []);
        var funcDef = functions.FirstOrDefault(x => x.Name == functionName);
        if (!string.IsNullOrWhiteSpace(funcDef?.Output))
        {
            return new DummyFunctionExecutor(_services, funcDef);
        }

        var mcpServerId = agent?.McpTools?.Where(x => x.Functions.Any(y => y.Name == funcDef?.Name))?.FirstOrDefault()?.ServerId;
        if (!string.IsNullOrWhiteSpace(mcpServerId))
        {
            return new McpToolExecutor(_services, mcpServerId, functionName);
        }

        return null;
    }
}
