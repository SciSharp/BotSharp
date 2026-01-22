using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules;

public interface IRuleAction
{
    string Provider { get; }

    Task<string> SendChatAsync(Agent agent, RuleChatActionPayload payload)
        => throw new NotImplementedException();

    Task<bool> SendHttpRequestAsync()
        => throw new NotImplementedException();

    Task<bool> SendEventMessageAsync(RuleDelay delay, RuleEventMessageOptions? options)
        => throw new NotImplementedException();

    Task<bool> ExecuteMethodAsync(Agent agent, Func<Agent, Task> func)
        => throw new NotImplementedException();
}
