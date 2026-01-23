using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Rules;
using BotSharp.Core.NRules.Hooks;
using BotSharp.Core.NRules.Services;
using BotSharp.Core.Rules.Engines;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NRules;
using NRules.RuleSharp;

namespace BotSharp.Core.NRules;

public class NRulesPlugin : IBotSharpPlugin
{
    public string Id => "d21c29e0-7f04-9885-fa0e-aca1f021011f";
    public string Name => "BotSharp Universal Parsing Engine";
    public string Description => "";


    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        // 1. Register rule repository (singleton)
        services.AddSingleton<RuleRepository>(provider =>
        {
            var repo = new RuleRepository();
            // Key: Inject references to BotSharp core and abstraction layer assemblies
            repo.AddReference(typeof(BotSharp.Abstraction.Rules.IRuleEngine).Assembly);
            repo.AddReference(typeof(BotSharp.Core.NRules.Services.UniversalParsingEngine).Assembly);
            // Inject commonly used system libraries
            repo.AddNamespace("System");
            repo.AddNamespace("System.Linq");
            return repo;
        });

        // 2. Register rule loader service
        services.AddSingleton<IRuleLoader, RuleSharpFileLoader>();

        // 3. Register compiled session factory (singleton)
        services.AddSingleton<ISessionFactory>(provider =>
        {
            var repo = provider.GetRequiredService<RuleRepository>();
            var loader = provider.GetRequiredService<IRuleLoader>();
            // Load all .rs files from the specified directory
            loader.LoadFromDirectory(repo, "Settings/Rules");
            return repo.Compile();
        });

        // 5. Register Hook to intercept conversation
        services.AddScoped<IConversationHook, RuleInjectionHook>();

        services.AddScoped<IRuleExecutor, NRulesExecutor>();
        services.AddScoped<IUniversalParsingEngine, UniversalParsingEngine>();
    }
}