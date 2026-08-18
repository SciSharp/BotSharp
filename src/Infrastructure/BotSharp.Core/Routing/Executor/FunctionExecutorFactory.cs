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
        // This is the one place in the repo that decides who executes a given function name (see
        // the interface's own doc comment), so it cannot rely on every caller validating first --
        // RoutingService.InvokeFunction has no guard at all and passes `name` straight through. A
        // null/blank function name reaching one of the registered IFunctionExecutorProvider
        // implementations below would make some of them throw ArgumentNullException instead of
        // returning null the way "no such function" does: a mock/blocking provider doing a
        // Dictionary lookup by name, for one, and ToolCallActionTests' NullIntolerantProvider
        // already proves that shape exists. Guarding once here saves every caller from repeating
        // the same check.
        if (string.IsNullOrWhiteSpace(functionName))
        {
            return null;
        }

        // Give external providers first refusal. Order is stable (ascending Order), never the DI
        // registration order.
        var providers = _services.GetServices<IFunctionExecutorProvider>().OrderBy(x => x.Order);
        foreach (var provider in providers)
        {
            var claimed = provider.TryResolve(functionName, agent);
            if (claimed != null)
            {
                return claimed;
            }
        }

        // The three stages below are the pre-existing logic verbatim -- same order, same semantics.
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
