using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public interface IInstructHook : IHookBase
{
    Task BeforeCompletion(Agent agent, RoleDialogModel message);
    Task AfterCompletion(Agent agent, InstructResult result);
    Task OnResponseGenerated(InstructResponseModel response);
}
