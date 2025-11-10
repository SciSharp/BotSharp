using BotSharp.Abstraction.FuzzSharp;
using BotSharp.Abstraction.Knowledges;
using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.FuzzySharp.Services;
using BotSharp.Plugin.FuzzySharp.Services.Matching;
using BotSharp.Plugin.FuzzySharp.Services.Processors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.FuzzySharp;

public class FuzzySharpPlugin : IBotSharpPlugin
{
    public string Id => "379e6f7b-c58c-458b-b8cd-0374e5830711";
    public string Name => "Fuzzy Sharp";
    public string Description => "Analyze text for typos and entities using domain-specific vocabulary.";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/512/9592/9592995.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<INgramProcessor, NgramProcessor>();
        services.AddScoped<IResultProcessor, ResultProcessor>();
        services.AddScoped<ITextAnalysisService, TextAnalysisService>();
        services.AddScoped<IPhraseCollection, CsvPhraseCollectionLoader>();
        services.AddScoped<ITokenMatcher, ExactMatcher>();
        services.AddScoped<ITokenMatcher, DomainTermMatcher>();
        services.AddScoped<ITokenMatcher, FuzzyMatcher>();
    }
}
