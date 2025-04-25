namespace BotSharp.Core.Crontab.Abstraction;

public abstract class ICrontabHook
{
    public string[]? Triggers
        => null;

    public virtual void OnAuthenticate(CrontabItem item)
    {
    }

    public Task OnCronTriggered(CrontabItem item)
        => Task.CompletedTask;

    public Task OnTaskExecuting(CrontabItem item)
        => Task.CompletedTask;

    public Task OnTaskExecuted(CrontabItem item)
        => Task.CompletedTask;
}
