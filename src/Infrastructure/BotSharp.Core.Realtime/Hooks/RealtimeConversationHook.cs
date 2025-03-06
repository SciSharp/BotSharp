using BotSharp.Abstraction.Utilities;

namespace BotSharp.Core.Realtime.Hooks;

public class RealtimeConversationHook : ConversationHookBase, IConversationHook
{
    private readonly IServiceProvider _services;
    public RealtimeConversationHook(IServiceProvider services)
    {
        _services = services;
    }

    public async Task OnFunctionExecuting(RoleDialogModel message)
    {
        var hub = _services.GetRequiredService<IRealtimeHub>();
        if (hub.HubConn == null)
        {
            return;
        }
        // Save states
        var states = _services.GetRequiredService<IConversationStateService>();
        states.SaveStateByArgs(message.FunctionArgs?.JsonContent<JsonDocument>() ?? JsonDocument.Parse("{}"));
    }

    public async Task OnFunctionExecuted(RoleDialogModel message)
    {
        var hub = _services.GetRequiredService<IRealtimeHub>();
        if (hub.HubConn == null)
        {
            return;
        }
        var routing = _services.GetRequiredService<IRoutingService>();

        message.Role = AgentRole.Function;

        if (message.FunctionName == "route_to_agent")
        {
            var inst = JsonSerializer.Deserialize<RoutingArgs>(message.FunctionArgs ?? "{}") ?? new();
            message.Content = $"Connected to agent of {inst.AgentName}";
            hub.HubConn.CurrentAgentId = routing.Context.GetCurrentAgentId();

            await hub.Completer.UpdateSession(hub.HubConn);
            await hub.Completer.InsertConversationItem(message);
            await hub.Completer.TriggerModelInference($"Guide the user through the next steps of the process as this Agent ({inst.AgentName}), following its instructions and operational procedures.");
        }
        else if (message.FunctionName == "util-routing-fallback_to_router")
        {
            var inst = JsonSerializer.Deserialize<FallbackArgs>(message.FunctionArgs ?? "{}") ?? new();
            message.Content = $"Returned to Router due to {inst.Reason}";
            hub.HubConn.CurrentAgentId = routing.Context.GetCurrentAgentId();

            await hub.Completer.UpdateSession(hub.HubConn);
            await hub.Completer.InsertConversationItem(message);
            await hub.Completer.TriggerModelInference($"Check with user whether to proceed the new request: {inst.Reason}");
        }
        else
        {
            // Update session for changed states
            await hub.Completer.UpdateSession(hub.HubConn);
            await hub.Completer.InsertConversationItem(message);
            await hub.Completer.TriggerModelInference("Reply based on the function's output.");
        }
    }
}
