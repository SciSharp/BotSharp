using BotSharp.Abstraction.Functions;

namespace BotSharp.Core.Routing.Executor;

internal class FunctionExecutorFactory
{
    public static IFunctionExecutor Create(string functionName, Agent agent, IFunctionCallback functioncall, IServiceProvider serviceProvider)
    {
        if(functioncall != null)
        {
            return new FunctionCallbackExecutor(functioncall);
        }

        var funDef = agent?.Functions?.FirstOrDefault(x => x.Name == functionName);
        if (funDef != null)
        {
            if (!string.IsNullOrWhiteSpace(funDef?.Output))
            {
                return new DummyFunctionExecutor(funDef,serviceProvider);
            }                
        }
        else
        {
            funDef = agent?.SecondaryFunctions?.FirstOrDefault(x => x.Name == functionName);
            if (funDef != null)
            {
                if (!string.IsNullOrWhiteSpace(funDef?.Output))
                {
                    return new DummyFunctionExecutor(funDef, serviceProvider);
                }
                else
                {
                    var mcpServerId  = agent?.McpTools?.Where(x => x.Functions.Any(y => y.Name == funDef.Name))
                        .FirstOrDefault().ServerId;
                    return new MCPFunctionExecutor(mcpServerId, functionName, serviceProvider);
                }
            }
        }
        return null;
    }
}
