using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Rules;
using BotSharp.Core.NRules.Models;
using NRules;

namespace BotSharp.Core.NRules.Services;

/// <summary>
/// BotsharpRuleContext concrete implementation
/// </summary>
public class RuleContext : IRuleContext
{
    private readonly ISession _session;
    private readonly IConversationStateService _stateService;
    private readonly string _conversationId;

    public RuleContext(ISession session, IConversationStateService stateService, string conversationId)
    {
        _session = session;
        _stateService = stateService;
        _conversationId = conversationId;
    }

    public ISession Session => _session;

    public void Insert<T>(T fact) => _session.Insert(fact);

    public void InsertAll(IEnumerable<object> facts) => _session.InsertAll(facts);

    public int Fire() => _session.Fire();

    public void Retract(object fact) => _session.Retract(fact);

    public async Task HydrateFactsAsync()
    {
        // 1. Get all states from BotSharp
        var states = _stateService.GetStates();

        // 2. Use FactMapper (custom implementation required) to convert Dictionary to POCO
        // Example: map "user_level": "vip" to a Customer object
        var facts = FactMapper.Map(states);

        // 3. Insert into session
        _session.InsertAll(facts);
    }

    public async Task PersistStateAsync()
    {
        // 1. Query specific state update objects (BotStateUpdate) from working memory
        var updates = _session.Query<BotStateUpdate>();

        // 2. Write back to BotSharp
        foreach (var update in updates)
        {
            _stateService.SetState(update.Key, update.Value);
        }

        // 3. Clean up transient facts
        // (Optional strategy: retract all facts inserted in this round to keep the session clean, relying on external persistence)
    }
}