using Microsoft.Bot.Schema;

namespace BotSharp.Plugin.MicrosoftTeams.Services;

/// <summary>
/// Persists the <see cref="ConversationReference"/> captured from inbound activities so the bot
/// can later push proactive (unsolicited) messages back to the same Teams user / channel.
/// The in-memory implementation is fine for a single node; replace it with a durable store
/// (BotSharp storage, Mongo, Redis) for multi-instance deployments.
/// </summary>
public interface IConversationReferenceStore
{
    Task SaveAsync(string userId, ConversationReference reference);
    Task<ConversationReference?> GetAsync(string userId);
    Task<IReadOnlyCollection<ConversationReference>> GetAllAsync();
}
