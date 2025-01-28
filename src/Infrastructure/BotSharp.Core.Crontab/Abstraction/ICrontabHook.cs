namespace BotSharp.Core.Crontab.Abstraction;

public interface ICrontabHook
{
    string[]? Triggers
        => null;

    Task OnCronTriggered(CrontabItem item)
        => Task.CompletedTask;

    Task OnTaskExecuting(CrontabItem item)
        => Task.CompletedTask;

    Task OnTaskExecuted(CrontabItem item)
        => Task.CompletedTask;
}
