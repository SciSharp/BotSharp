using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Planning;

/// <summary>
/// Task breakdown and execution plan 
/// </summary>
public interface IPlaner
{
    Task<FunctionCallFromLlm> GetNextInstruction(Agent router);
    Task<bool> AgentExecuting(FunctionCallFromLlm inst, RoleDialogModel message);
    Task<bool> AgentExecuted(FunctionCallFromLlm inst, RoleDialogModel message);
}
