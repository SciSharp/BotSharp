using Refit;

namespace BotSharp.Plugin.MetaMessenger;

/// <summary>
/// https://developers.facebook.com/docs/messenger-platform/overview
/// </summary>
public class MetaMessengerPlugin : IBotSharpPlugin
{
    public string Id => "3694d49c-ddc4-43ea-b019-4d3b373ad570";
    public string Name => "Meta Messenger";
    public string Description => "Messaging service that allows users to connect with others and share content.";
    public string IconUrl => "https://static.xx.fbcdn.net/rsrc.php/yJ/r/C1E_YZIckM5.svg";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new MetaMessengerSetting();
        config.Bind("MetaMessenger", settings);
        services.AddSingleton(x =>
        {
            Console.WriteLine($"Loaded MetaMessenger settings: {settings.Endpoint}/{settings.ApiVersion} {settings.PageId} {settings.PageAccessToken.SubstringMax(4)}");
            return settings;
        });

        // services.AddScoped<AuthHeaderHandler>();
        services.AddRefitClient<IMessengerGraphAPI>()
            .ConfigureHttpClient((sp, c) =>
                {
                    var setting = sp.GetRequiredService<MetaMessengerSetting>();
                    c.BaseAddress = new Uri($"{setting.Endpoint}");
                });
        //.AddHttpMessageHandler<AuthHeaderHandler>();

        services.AddScoped<MessageHandleService>();
    }
}
