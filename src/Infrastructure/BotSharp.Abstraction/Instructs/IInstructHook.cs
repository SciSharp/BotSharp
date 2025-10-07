using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public interface IInstructHook : IHookBase
{
    Task BeforeCompletion(Agent agent, RoleDialogModel message) => Task.CompletedTask;
    Task AfterCompletion(Agent agent, InstructResult result) => Task.CompletedTask;
    Task OnResponseGenerated(InstructResponseModel response) => Task.CompletedTask;

    Task BeforeCodeExecution(Agent agent, RoleDialogModel message, CodeInstructContext context) => Task.CompletedTask;
    Task AfterCodeExecution(Agent agent, InstructResult result) => Task.CompletedTask;
}
