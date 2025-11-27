namespace BotSharp.Abstraction.Infrastructures;

public interface IDistributedLocker
{
    bool Lock(string resource, Action action, int timeout = 30, int acquireTimeout = default);
    Task<bool> LockAsync(string resource, Func<Task> action, int timeout = 30, int acquireTimeout = default);
}
