using BotSharp.Core.Crontab.Hooks;
using Microsoft.Extensions.Logging;

namespace BotSharp.Core.Crontab.Functions;

public class TaskWaitFn : IFunctionCallback
{
    public string Name => $"{CrontabUtilityHook.PREFIX}task_wait";

    private readonly ILogger<TaskWaitFn> _logger;
    public TaskWaitFn(ILogger<TaskWaitFn> logger)
    {
        _logger = logger;
    }
    public async Task<bool> Execute(RoleDialogModel message)
    {
        try
        {
            var args = JsonSerializer.Deserialize<TaskWaitArgs>(message.FunctionArgs);
            if (args != null && args.DelayTime > 0)
            {
                await Task.Delay(args.DelayTime * 1000);
            }
            message.Content = "wait task completed";
        }
        catch (JsonException jsonEx)
        {
            message.Content = "Invalid function arguments format.";
            _logger.LogError(jsonEx, "Json deserialization failed.");
        }
        catch (Exception ex)
        {
            message.Content = "Unable to perform delay task";
            _logger.LogError(ex, "crontab wait task failed.");
        }
        return true;
    }
}
