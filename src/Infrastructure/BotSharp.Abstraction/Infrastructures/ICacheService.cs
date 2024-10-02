namespace BotSharp.Abstraction.Infrastructures;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task<object> GetAsync(string key, Type type);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry);
    Task RemoveAsync(string key);
}
