using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public interface IInstructService
{
    Task<InstructResult> Execute(Agent agent, RoleDialogModel message);
}
