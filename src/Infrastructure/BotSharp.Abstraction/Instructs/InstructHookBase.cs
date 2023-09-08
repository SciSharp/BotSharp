using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public class InstructHookBase : IInstructHook
{
    public virtual async Task AfterCompletion(InstructResult result)
    {
        return;
    }

    public virtual async Task BeforeCompletion(RoleDialogModel message)
    {
        return;
    }
}
