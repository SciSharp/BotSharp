using BotSharp.Abstraction.Diagnostics;
using BotSharp.Abstraction.Diagnostics.Telemetry;
using BotSharp.Abstraction.Functions;
using BotSharp.Abstraction.Routing.Executor;
using System.Diagnostics;
using static BotSharp.Abstraction.Diagnostics.Telemetry.TelemetryConstants;

namespace BotSharp.Core.Routing.Executor;

public  class FunctionCallbackExecutor : IFunctionExecutor
{
    private readonly IFunctionCallback _functionCallback;
    private readonly ITelemetryService _telemetryService;

    public FunctionCallbackExecutor(ITelemetryService telemetryService, IFunctionCallback functionCallback)
    {
        _functionCallback = functionCallback;
        _telemetryService = telemetryService;
    }

    public async Task<bool> ExecuteAsync(RoleDialogModel message)
    {
        using var activity = _telemetryService.Parent.StartFunctionActivity(this._functionCallback.Name, this._functionCallback.Indication);
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
