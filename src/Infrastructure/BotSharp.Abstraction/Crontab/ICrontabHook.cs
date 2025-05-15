using BotSharp.Abstraction.Hooks;

namespace BotSharp.Abstraction.Crontab;

public interface ICrontabHook : IHookBase
{
    string[]? Triggers
        => null;

    void OnAuthenticate(CrontabItem item)
    {
    }

    Task OnCronTriggered(CrontabItem item)
        => Task.CompletedTask;

    Task OnTaskExecuting(CrontabItem item)
        => Task.CompletedTask;

    Task OnTaskExecuted(CrontabItem item)
        => Task.CompletedTask;
}
