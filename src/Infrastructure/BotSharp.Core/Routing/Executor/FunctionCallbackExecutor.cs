using BotSharp.Abstraction.Functions;

namespace BotSharp.Core.Routing.Executor;

public  class FunctionCallbackExecutor : IFunctionExecutor
{
    IFunctionCallback functionCallback;

    public FunctionCallbackExecutor(IFunctionCallback functionCallback)
    {
        this.functionCallback = functionCallback;
    }

    public async Task<bool> ExecuteAsync(RoleDialogModel message)
    {
        return await functionCallback.Execute(message);
    }

    public async Task<string> GetIndicatorAsync(RoleDialogModel message)
    {
       return await functionCallback.GetIndication(message);
    }
}   
