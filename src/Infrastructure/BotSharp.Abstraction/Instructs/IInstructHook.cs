using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public interface IInstructHook
{
    Task BeforeCompletion(RoleDialogModel message);
    Task AfterCompletion(InstructResult result);
}
