using BotSharp.Abstraction.Instructs.Models;

namespace BotSharp.Abstraction.Instructs;

public interface IInstructService
{
    Task<InstructResult> Execute(string agentId, RoleDialogModel message, string? templateName = null, string? instruction = null);
}
