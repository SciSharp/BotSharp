namespace BotSharp.Abstraction.Conversations;

/// <summary>
/// Reports whether a conversation is synthetic -- driven by an automated harness rather than by a
/// person -- so that guards aimed at human overuse can stand aside for it.
///
/// This exists because per-user volume limits count the wrong thing for a test harness. A regression
/// suite legitimately opens one conversation per case per model, at machine speed, and does it under
/// whatever identity the background worker happens to have. Measured as human behaviour that looks
/// like abuse, and the limit then fails every test with a message about conversation quotas -- which
/// says nothing about the agent under test and is indistinguishable, in a report, from the agent
/// having regressed.
///
/// Deliberately a query rather than a flag on the message: a flag would have to be threaded through
/// every call that creates or continues a conversation, and anything that forgot would silently be
/// treated as human traffic. Asking by conversation id keeps the answer in one place.
///
/// Implementations must be cheap and side-effect free -- this is called on the message path -- and
/// must answer false for anything they do not recognise. Several may be registered; a conversation is
/// synthetic if any of them claims it. None being registered is the normal case, and then nothing is
/// exempt.
/// </summary>
public interface ISyntheticConversationProbe
{
    /// <summary>
    /// True when this conversation is being driven by a harness. Must not throw, and must not treat
    /// an unknown or blank id as synthetic: getting this wrong in that direction exempts real user
    /// traffic from the very limits it is meant to be held to.
    /// </summary>
    bool IsSynthetic(string conversationId);
}
