using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotSharp.Abstraction.Rules;

public interface IRuleExecutor
{
    /// <summary>
    /// Execute rules on the given facts.
    /// </summary>
    /// <param name="facts">The objects to assert into the rule engine.</param>
    /// <param name="ruleSets">Optional names of rule sets to activate.</param>
    /// <returns>The modified facts or execution results.</returns>
    Task ExecuteAsync(IEnumerable<object> facts, IEnumerable<string>? ruleSets = null);
}
