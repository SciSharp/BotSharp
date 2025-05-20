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
        
        if (message.FunctionName == "response_to_user")
        {
            return;
        }

        // Save states
        if (message.FunctionArgs != null && message.FunctionArgs.Length > 3)
        {
            var states = _services.GetRequiredService<IConversationStateService>();
            states.SaveStateByArgs(message.FunctionArgs?.JsonContent<JsonDocument>());
        }
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
            hub.HubConn.CurrentAgentId = routing.Context.GetCurrentAgentId();

            await hub.Completer.UpdateSession(hub.HubConn);
            await hub.Completer.TriggerModelInference();
        }
        else if (message.FunctionName == "util-routing-fallback_to_router")
        {
            hub.HubConn.CurrentAgentId = routing.Context.GetCurrentAgentId();

            await hub.Completer.UpdateSession(hub.HubConn);
            await hub.Completer.TriggerModelInference();
        }
        else if (message.FunctionName == "response_to_user")
        {
            await hub.Completer.InsertConversationItem(message);
            await hub.Completer.TriggerModelInference();
        }
        else
        {
            // Update session for changed states
            var instruction = await hub.Completer.UpdateSession(hub.HubConn);
            await hub.Completer.InsertConversationItem(message);

            if (string.IsNullOrEmpty(message.Content))
            {
                return;
            }

            if (message.StopCompletion)
            {
                await hub.Completer.TriggerModelInference($"Say to user: \"{message.Content}\"");
            }
            else
            {
                await hub.Completer.TriggerModelInference(instruction);
            }
        }
    }
}
