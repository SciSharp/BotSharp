using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public class InstructHookBase : IInstructHook
{
    public virtual string SelfId => throw new NotImplementedException("Please set SelfId as agent id!");

    public virtual async Task BeforeCompletion(Agent agent, RoleDialogModel message)
    {
        await Task.CompletedTask;
    }

    public virtual async Task AfterCompletion(Agent agent, InstructResult result)
    {
        await Task.CompletedTask;
    }

    public virtual async Task OnResponseGenerated(InstructResponseModel response)
    {
        await Task.CompletedTask;
    }
}
