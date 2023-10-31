using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public interface IInstructHook
{
    string SelfId { get; }
    Task BeforeCompletion(Agent agent, RoleDialogModel message);
    Task AfterCompletion(Agent agent, InstructResult result);
}
