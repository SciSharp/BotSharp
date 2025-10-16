using BotSharp.Abstraction.Diagnostics;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing.Executor;
using System.Diagnostics;
using static BotSharp.Abstraction.Diagnostics.ModelDiagnostics;

namespace BotSharp.Core.Routing.Executor;

public  class FunctionCallbackExecutor : IFunctionExecutor
{
    /// <summary>
    /// <see cref="ActivitySource"/>
    /// for function-related activities.
    /// </summary>
    private static readonly ActivitySource s_activitySource = new("BotSharp.Core.Routing.Executor");

    private readonly IFunctionCallback _functionCallback;

    public FunctionCallbackExecutor(IFunctionCallback functionCallback)
    {
        _functionCallback = functionCallback;
    }

    public async Task<bool> ExecuteAsync(RoleDialogModel message)
    {
        using var activity = s_activitySource.StartFunctionActivity(this._functionCallback.Name, this._functionCallback.Indication);
        {
            activity?.SetTag("input", message.FunctionArgs);
            activity?.SetTag(ModelDiagnosticsTags.AgentId, message.CurrentAgentId);
            return await _functionCallback.Execute(message);
        }
    }

    public async Task<string> GetIndicatorAsync(RoleDialogModel message)
    {
       return await _functionCallback.GetIndication(message);
    }
}   
