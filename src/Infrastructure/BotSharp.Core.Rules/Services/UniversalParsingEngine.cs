using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Options;
using BotSharp.Abstraction.Rules;
using System.Threading.Tasks;

namespace BotSharp.Core.Rules.Services;

public class UniversalParsingEngine : IUniversalParsingEngine
{
    private readonly IInstructService _instructService;
    private readonly IRuleExecutor _ruleExecutor;

    public UniversalParsingEngine(IInstructService instructService, IRuleExecutor ruleExecutor)
    {
        _instructService = instructService;
        _ruleExecutor = ruleExecutor;
    }

    public async Task<T?> ParseAsync<T>(string text, InstructOptions? options = null) where T : class
    {
        // 1. Generate Fact using LLM
        var fact = await _instructService.Instruct<T>(text, options);
        
        if (fact != null)
        {
            // 2. Execute Rules
            await _ruleExecutor.ExecuteAsync(new[] { fact });
        }

        return fact;
    }
}
