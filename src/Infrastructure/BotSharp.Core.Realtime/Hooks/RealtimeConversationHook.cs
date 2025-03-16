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
            message.Content = $"I'm your AI assistant '{inst.AgentName}' to help with: '{inst.NextActionReason}'";
            hub.HubConn.CurrentAgentId = routing.Context.GetCurrentAgentId();

            var instruction = await hub.Completer.UpdateSession(hub.HubConn);
            await hub.Completer.InsertConversationItem(message);
            await hub.Completer.TriggerModelInference($"{instruction}\r\n\r\nAssist user task: {inst.NextActionReason}");
        }
        else if (message.FunctionName == "util-routing-fallback_to_router")
        {
            var inst = JsonSerializer.Deserialize<FallbackArgs>(message.FunctionArgs ?? "{}") ?? new();
            message.Content = $"Returned to Router due to {inst.Reason}";
            hub.HubConn.CurrentAgentId = routing.Context.GetCurrentAgentId();

            var instruction = await hub.Completer.UpdateSession(hub.HubConn);
            await hub.Completer.InsertConversationItem(message);
            await hub.Completer.TriggerModelInference(instruction);
        }
        else
        {
            // Update session for changed states
            var instruction = await hub.Completer.UpdateSession(hub.HubConn);
            await hub.Completer.InsertConversationItem(message);
            await hub.Completer.TriggerModelInference(instruction);
        }
    }
}
