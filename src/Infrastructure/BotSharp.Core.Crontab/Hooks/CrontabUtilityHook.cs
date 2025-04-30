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
                Name = UtilityName.ScheduleTask,
                Functions = [new(SCHEDULE_TASK_FN), new(TASK_WAIT_FN)],
                Templates = [new($"{SCHEDULE_TASK_FN}.fn")]
            }
        };

        utilities.AddRange(items);
    }
}
