namespace BotSharp.Logger;

public static class BotSharpLoggerExtensions
{
    public static IServiceCollection AddBotSharpLogger(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IContentGeneratingHook, CommonContentGeneratingHook>();
        services.AddScoped<IContentGeneratingHook, TokenStatsConversationHook>();
        services.AddScoped<IContentGeneratingHook, VerboseLogHook>();
        return services;
    }
}
