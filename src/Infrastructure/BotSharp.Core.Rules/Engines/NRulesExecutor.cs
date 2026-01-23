using BotSharp.Abstraction.Rules;
using NRules;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BotSharp.Core.Rules.Engines;

public class NRulesExecutor : IRuleExecutor
{
    private readonly ISessionFactory _factory;

    public NRulesExecutor(ISessionFactory factory)
    {
        _factory = factory;
    }

    public Task ExecuteAsync(IEnumerable<object> facts, IEnumerable<string>? ruleSets = null)
    {
        var session = _factory.CreateSession();
        foreach (var fact in facts)
        {
            session.Insert(fact);
        }
        
        // In the future we can filter by ruleSets if NRules supports it or via filtering
        session.Fire();
        return Task.CompletedTask;
    }
}
