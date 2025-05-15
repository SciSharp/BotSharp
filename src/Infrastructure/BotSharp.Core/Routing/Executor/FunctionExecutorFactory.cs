using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing.Executor;

namespace BotSharp.Core.Routing.Executor;

internal class FunctionExecutorFactory
{
    public static IFunctionExecutor? Create(IServiceProvider services, string functionName, Agent agent)
    {
        var functionCall = services.GetServices<IFunctionCallback>().FirstOrDefault(x => x.Name == functionName);
        if (functionCall != null)
        {
            return new FunctionCallbackExecutor(functionCall);
        }

        var functions = (agent?.Functions ?? []).Concat(agent?.SecondaryFunctions ?? []);
        var funcDef = functions.FirstOrDefault(x => x.Name == functionName);
        if (!string.IsNullOrWhiteSpace(funcDef?.Output))
        {
            return new DummyFunctionExecutor(services, funcDef);
        }

        var mcpServerId = agent?.McpTools?.Where(x => x.Functions.Any(y => y.Name == funcDef?.Name))?.FirstOrDefault()?.ServerId;
        if (!string.IsNullOrWhiteSpace(mcpServerId))
        {
            return new McpToolExecutor(services, mcpServerId, functionName);
        }
        
        return null;
    }
}
