using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Planning;

/// <summary>
/// Task breakdown and execution plan 
/// </summary>
public interface IPlaner
{
    Task<FunctionCallFromLlm> GetNextInstruction(Agent router, string conversation);
    Task<bool> AgentExecuted(FunctionCallFromLlm inst, RoleDialogModel message);
}
