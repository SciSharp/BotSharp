using BotSharp.Abstraction.Agents.Models;
using BotSharp.Core.Crontab.Enum;

namespace BotSharp.Core.Crontab.Hooks;

public class CrontabUtilityHook : IAgentUtilityHook
{
    public const string PREFIX = "util-crontab-";
    private const string SCHEDULE_TASK_FN = $"{PREFIX}schedule_task";
    private const string TASK_WAIT_FN = $"{PREFIX}task_wait";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var items = new List<AgentUtility>
        {
            new AgentUtility
            {
                Category = "crontab",
                Name = UtilityName.ScheduleTask,
                Items = [
                    new UtilityItem
                    {
                        FunctionName = SCHEDULE_TASK_FN,
                        TemplateName = $"{SCHEDULE_TASK_FN}.fn"
                    },
                    new UtilityItem
                    {
                        FunctionName = TASK_WAIT_FN
                    },
                ]
            }
        };

        utilities.AddRange(items);
    }
}
