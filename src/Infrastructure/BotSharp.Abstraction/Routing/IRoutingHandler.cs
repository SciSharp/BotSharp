using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Routing;

public interface IRoutingHandler
{
    string Name { get; }
    string Description { get; }
    bool IsReasoning { get; }
    bool RequireAgent { get; }
    List<string> Parameters { get; }
    void SetRouter(Agent router);
    void SetDialogs(List<RoleDialogModel> dialogs);
    Task<FunctionCallFromLlm> GetNextInstructionFromReasoner(string prompt);
    Task<RoleDialogModel> GetResponseFromReasoner();
    Task<RoleDialogModel> Handle(FunctionCallFromLlm inst);
}
