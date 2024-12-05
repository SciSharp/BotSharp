using BotSharp.Abstraction.Agents.Models;
using BotSharp.Core.Crontab.Enum;

namespace BotSharp.Core.Crontab.Hooks;

public class CrontabUtilityHook : IAgentUtilityHook
{
    private const string PREFIX = "util-crontab-";
    private const string SCHEDULE_TASK_FN = $"{PREFIX}schedule_task";
    
    public void AddUtilities(List<AgentUtility> utilities)
    {
        var items = new List<AgentUtility>
        {
            new AgentUtility
            {
                Name = UtilityName.ScheduleTask,
                Functions = [new(SCHEDULE_TASK_FN)],
                Templates = [new($"{SCHEDULE_TASK_FN}.fn")]
            }
        };

        utilities.AddRange(items);
    }
}
