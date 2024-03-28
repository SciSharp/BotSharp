namespace BotSharp.Plugin.MetaGLM;

public class MetaGLMPlugin : IBotSharpPlugin
{
    public string Id => "35d464d9-dd94-4cac-9e5a-1eaff6b943f5";

    public string Name => "MetaGLM AI";

    public string Description => "MetaGLM Service including text generation and embedding services.";

    public SettingsMeta Settings => new SettingsMeta("MetaGLM");

    public object GetNewSettingsInstance()
    {
        return new MetaGLMSettings();
    }


    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<MetaGLMSettings>("MetaGLM");
        });
        services.AddScoped<MetaGLMClient>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
    }
}
