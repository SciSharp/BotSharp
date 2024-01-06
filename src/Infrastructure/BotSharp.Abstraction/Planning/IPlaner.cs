using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Planning;

/// <summary>
/// Task breakdown and execution plan 
/// https://www.promptingguide.ai/techniques/cot
/// </summary>
public interface IPlaner
{
    Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string messageId);
    Task<bool> AgentExecuting(Agent router, FunctionCallFromLlm inst, RoleDialogModel message);
    Task<bool> AgentExecuted(Agent router, FunctionCallFromLlm inst, RoleDialogModel message);
}
