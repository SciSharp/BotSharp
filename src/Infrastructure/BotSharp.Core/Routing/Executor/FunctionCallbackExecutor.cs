using BotSharp.Abstraction.Routing.Executor;
using BotSharp.Abstraction.Functions;

namespace BotSharp.Core.Routing.Executor;

public  class FunctionCallbackExecutor : IFunctionExecutor
{
    private readonly IFunctionCallback _functionCallback;

    public FunctionCallbackExecutor(IFunctionCallback functionCallback)
    {
        _functionCallback = functionCallback;
    }

    public async Task<bool> ExecuteAsync(RoleDialogModel message)
    {
        return await _functionCallback.Execute(message);
    }

    public async Task<string> GetIndicatorAsync(RoleDialogModel message)
    {
       return await _functionCallback.GetIndication(message);
    }
}   
