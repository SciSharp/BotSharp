using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Routing;

/// <summary>
/// The routing handler will be injected to Router's FUNCTIONS section of the system prompt
/// So the handler will be invoked by LLM autonomously.
/// </summary>
public interface IRoutingHandler
{
    string Name { get; }
    string Description { get; }
    List<string> Planers => null;
    bool Enabled => true;
    List<ParameterPropertyDef> Parameters => new List<ParameterPropertyDef>();

    void SetDialogs(List<RoleDialogModel> dialogs);

    Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message);
}
