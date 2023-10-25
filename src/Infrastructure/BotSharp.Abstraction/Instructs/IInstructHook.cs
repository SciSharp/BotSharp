using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public interface IInstructHook
{
    string SelfId { get; }
    Task BeforeCompletion(RoleDialogModel message);
    Task AfterCompletion(InstructResult result);
}
