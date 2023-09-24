using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction.Routing;

public interface IRoutingHandler
{
    string Name { get; }
    string Description { get; }
    bool IsReasoning { get => false; }
    bool Enabled { get => true; }
    List<NameDesc> Parameters { get => new List<NameDesc>(); }

    void SetRouter(Agent router) { }

    void SetDialogs(List<RoleDialogModel> dialogs) { }        

    Task<FunctionCallFromLlm> GetNextInstructionFromReasoner(string prompt) 
        => throw new NotImplementedException(""); 

    Task<RoleDialogModel> Handle(FunctionCallFromLlm inst)
        => throw new NotImplementedException("");
}
