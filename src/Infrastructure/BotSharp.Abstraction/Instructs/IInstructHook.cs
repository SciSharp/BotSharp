using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public interface IInstructHook
{
    string SelfId { get; }
    bool IsMatch(string id) => string.IsNullOrEmpty(SelfId) || SelfId == id;
    Task BeforeCompletion(Agent agent, RoleDialogModel message);
    Task AfterCompletion(Agent agent, InstructResult result);
    Task OnResponseGenerated(InstructResponseModel response);
}
