namespace BotSharp.Abstraction.Rules;

public interface IRuleConfig<T> where T : class
{
    /// <summary>
    /// Rule config provider
    /// </summary>
    string Provider { get; }

    /// <summary>
    /// Get rule topology
    /// </summary>
    /// <param name="id"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<T> GetTopologyAsync(string id, RuleConfigLoadOptions? options = null);
}
