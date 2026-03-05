namespace BotSharp.Abstraction.Rules;

public interface IRuleFlow<T> where T : class
{
    /// <summary>
    /// Rule flow provider
    /// </summary>
    string Provider { get; }

    /// <summary>
    /// Get rule flow topology
    /// </summary>
    /// <param name="id"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<T> GetTopologyAsync(string id, RuleFlowLoadOptions? options = null);
}
