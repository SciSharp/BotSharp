using System.Collections.Concurrent;
using Microsoft.Bot.Schema;

namespace BotSharp.Plugin.MicrosoftTeams.Services;

public class InMemoryConversationReferenceStore : IConversationReferenceStore
{
    private readonly ConcurrentDictionary<string, ConversationReference> _references = new();

    public Task SaveAsync(string userId, ConversationReference reference)
    {
        _references[userId] = reference;
        return Task.CompletedTask;
    }

    public Task<ConversationReference?> GetAsync(string userId)
    {
        _references.TryGetValue(userId, out var reference);
        return Task.FromResult(reference);
    }

    public Task<IReadOnlyCollection<ConversationReference>> GetAllAsync()
        => Task.FromResult((IReadOnlyCollection<ConversationReference>)_references.Values.ToList());
}
