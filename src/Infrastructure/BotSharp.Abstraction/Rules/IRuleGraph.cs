namespace BotSharp.Abstraction.Rules;

public interface IRuleGraph
{
    /// <summary>
    /// Rule graph provider
    /// </summary>
    string Provider { get; }

    /// <summary>
    /// Load graph
    /// </summary>
    /// <param name="graphId"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    Task<RuleGraph> GetGraphAsync(string graphId, RuleGraphLoadOptions? options = null);
}
