using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.Membase.Hooks;

public class MembaseUtilityHook : IAgentUtilityHook
{
    private const string ADVANCE_CURSOR_FN = "util-workflow-advance_workflow_cursor";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var items = new List<AgentUtility>
        {
            new AgentUtility
            {
                Category = "workflow",
                Name = "advance-workflow-cursor",
                Items = [
                    new UtilityItem
                    {
                        FunctionName = ADVANCE_CURSOR_FN
                    }
                ]
            }
        };

        utilities.AddRange(items);
    }
}
