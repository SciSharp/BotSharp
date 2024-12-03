using BotSharp.Core.Crontab.Models;

namespace BotSharp.Core.Crontab.Abstraction;

public interface ICrontabHook
{
    Task OnCronTriggered(CrontabItem item);
}
