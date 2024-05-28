using BotSharp.Abstraction.Infrastructures.Enums;

namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService : IConversationService
{
    public async Task UpdateBreakpoint(bool resetStates = false, string? reason = null)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        var routingCtx = _services.GetRequiredService<IRoutingContext>();
        var messageId = routingCtx.MessageId;

        db.UpdateConversationBreakpoint(_conversationId, new ConversationBreakpoint
        {
            MessageId = messageId,
            Breakpoint = DateTime.UtcNow,
            Reason = reason
        });

        // Reset states
        if (resetStates)
        {
            var states = _services.GetRequiredService<IConversationStateService>();
            // keep language state
            states.CleanStates(StateConst.LANGUAGE);
        }

        var hooks = _services.GetServices<IConversationHook>()
            .OrderBy(x => x.Priority)
            .ToList();

        // Before executing functions
        foreach (var hook in hooks)
        {
            await hook.OnBreakpointUpdated(_conversationId, resetStates);
        }
    }
}
