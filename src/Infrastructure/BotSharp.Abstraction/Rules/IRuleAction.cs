using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules;

public interface IRuleAction
{
    string Provider { get; }

    Task<string> SendChatAsync(Agent agent, RuleChatActionPayload payload)
        => throw new NotImplementedException();

    Task<bool> SendHttpRequestAsync()
        => throw new NotImplementedException();

    Task<bool> SendMessageAsync(RuleDelay delay, RuleMessagingOptions options)
        => throw new NotImplementedException();
}
