namespace BotSharp.Logger;

public static class BotSharpLoggerExtensions
{
    public static IServiceCollection AddBotSharpLogger(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IContentGeneratingHook, CommonContentGeneratingHook>();
        services.AddScoped<IContentGeneratingHook, TokenStatsConversationHook>();
        services.AddScoped<IVerboseLogHook, VerboseLogHook>();
        return services;
    }
}
