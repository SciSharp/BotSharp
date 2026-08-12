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
