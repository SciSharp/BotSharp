using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Options;
using BotSharp.Abstraction.Rules;
using Microsoft.Extensions.DependencyInjection;
using NRules;
using NRules.RuleSharp;

namespace BotSharp.Core.NRules.Services;

public class UniversalParsingEngine : IUniversalParsingEngine
{
    private readonly IInstructService _instructService;
    private readonly IRuleExecutor _ruleExecutor;
    private readonly ISessionFactory _sessionFactory;
    private readonly IServiceProvider _services;

    public UniversalParsingEngine(IInstructService instructService, IRuleExecutor ruleExecutor, ISessionFactory sessionFactory, IServiceProvider services)
    {
        _sessionFactory = sessionFactory;
        _services = services;    
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

    public async Task<IRuleContext> GetContextAsync(string conversationId)
    {
        // Create NRules Session
        var session = _sessionFactory.CreateSession();

        // Get BotSharp's state service
        var stateService = _services.GetRequiredService<IConversationStateService>();

        // Instantiate BotsharpRuleContext wrapper
        return new RuleContext(session, stateService, conversationId);
    }

    public async Task LoadRules()
    {
        var repo = _services.GetRequiredService<RuleRepository>();
        var loader = _services.GetRequiredService<IRuleLoader>();
        // Load all .rs files from the specified directory
        loader.LoadFromDirectory(repo, "Settings/Rules");
        repo.Compile();
    }
}
