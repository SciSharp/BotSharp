using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Functions;
using BotSharp.Plugin.Planner.TwoStaging.Models;
using System.Threading.Tasks;

namespace BotSharp.Plugin.Planner.Functions;

public class SecondaryStagePlanFn : IFunctionCallback
{
    public string Name => "plan_secondary_stage";

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var task = JsonSerializer.Deserialize<SecondaryBreakdownTask>(message.FunctionArgs);
        message.Content = task.SolutionQuestion;
        message.Content += $"\r\n\r\n=====\r\nUse tool of `knowledge_retrieval` for expert instructions.";
        return true;
    }
}
