namespace BotSharp.Core.Conversations.Services;

public partial class ConversationService : IConversationService
{
    public async Task UpdateBreakpoint()
    {
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.UpdateConversationBreakpoint(_conversationId, DateTime.UtcNow);
    }
}
