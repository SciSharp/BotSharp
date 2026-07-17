using BotSharp.Abstraction.Hooks;

namespace BotSharp.Abstraction.Crontab;

public interface ICrontabHook : IHookBase
{
    string[]? Triggers
        => null;

    /// <summary>
    /// Establishes the identity for the cron event; invoked synchronously for all hooks before any OnCronTriggered runs, so the identity flows down into the execution phase.
    /// </summary>
    /// <param name="item"></param>
    void OnAuthenticate(CrontabItem item) { }

    Task OnCronTriggered(CrontabItem item)
        => Task.CompletedTask;

    Task OnTaskExecuting(CrontabItem item)
        => Task.CompletedTask;

    Task OnTaskExecuted(CrontabItem item)
        => Task.CompletedTask;
}
