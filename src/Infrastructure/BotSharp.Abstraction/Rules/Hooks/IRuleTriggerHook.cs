using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Rules.Models;

namespace BotSharp.Abstraction.Rules.Hooks;

public interface IRuleTriggerHook : IHookBase
{
    Task BeforeSendEventMessage(Agent agent, RoleDialogModel message) => Task.CompletedTask;
    Task AfterSendEventMessage(Agent agent, InstructResult result) => Task.CompletedTask;

    Task BeforeSendHttpRequest(Agent agent, IRuleTrigger trigger, RuleHttpContext message) => Task.CompletedTask;
    Task AfterSendHttpRequest(Agent agent, IRuleTrigger trigger, RuleHttpResult result) => Task.CompletedTask;
}
