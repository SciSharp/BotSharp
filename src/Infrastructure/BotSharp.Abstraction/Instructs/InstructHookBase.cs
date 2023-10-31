using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public class InstructHookBase : IInstructHook
{
    public virtual string SelfId => throw new NotImplementedException("Please set SelfId as agent id!");

    public virtual async Task BeforeCompletion(Agent agent, RoleDialogModel message)
    {
        return;
    }

    public virtual async Task AfterCompletion(Agent agent, InstructResult result)
    {
        return;
    }
}
