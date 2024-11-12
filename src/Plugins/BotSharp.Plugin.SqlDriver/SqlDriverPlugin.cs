using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Planning;

namespace BotSharp.Plugin.SqlDriver;

public class SqlDriverPlugin : IBotSharpPlugin
{
    public string Id => "da7b6f7a-b1f0-455a-9939-ad2d493e929e";
    public string Name => "SQL Driver";
    public string Description => "Convert the user requirements into corresponding SQL statements";
    public string IconUrl => "https://uxwing.com/wp-content/themes/uxwing/download/file-and-folder-type/sql-icon.png";

    public string[] AgentIds =
    [
        BuiltInAgentId.SqlDriver
    ];

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<SqlDriverSetting>("SqlDriver");
        });

        services.AddScoped<SqlDriverService>();
        services.AddScoped<DbKnowledgeService>();
        services.AddScoped<IKnowledgeHook, SqlDriverKnowledgeHook>();
        services.AddScoped<IAgentHook, SqlExecutorHook>();
        services.AddScoped<IAgentUtilityHook, SqlUtilityHook>();
        services.AddScoped<IPlanningHook, SqlDriverPlanningHook>();
        services.AddScoped<IAgentHook, SqlDictionaryLookupHook>();
        services.AddScoped<IAgentHook, GetTableDefinitionHook>();
        services.AddScoped<IConversationHook, SqlDriverConversationHook>();
    }
}
