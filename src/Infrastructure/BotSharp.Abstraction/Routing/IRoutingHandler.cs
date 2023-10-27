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
    bool IsReasoning => false;
    bool Enabled => true;
    List<NameDesc> Parameters => new List<NameDesc>();

    void SetRouter(Agent router) { }

    void SetDialogs(List<RoleDialogModel> dialogs) { }

    Task<bool> Handle(IRoutingService routing, FunctionCallFromLlm inst, RoleDialogModel message);
}
