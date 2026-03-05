namespace BotSharp.Abstraction.Rules;

public interface IRuleConfig<T> where T : class
{
    /// <summary>
    /// Rule config provider
    /// </summary>
    string Provider { get; }

    /// <summary>
    /// Load config
    /// </summary>
    /// <param name="id"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<T> GetConfigAsync(string id, RuleConfigLoadOptions? options = null);
}
