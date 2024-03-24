namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService : IConversationService
{
    public async Task UpdateBreakpoint(bool resetStates = false)
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.UpdateConversationBreakpoint(_conversationId, DateTime.UtcNow);

        // Reset states
        if (resetStates)
        {
            var states = _services.GetRequiredService<IConversationStateService>();
            states.CleanStates();
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
