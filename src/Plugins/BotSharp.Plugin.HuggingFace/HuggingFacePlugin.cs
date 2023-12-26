using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.HuggingFace.Providers;
using BotSharp.Plugin.HuggingFace.Services;
using BotSharp.Plugin.HuggingFace.Settings;
using Refit;

namespace BotSharp.Plugin.HuggingFace;

public class HuggingFacePlugin : IBotSharpPlugin
{
    public string Name => "Hugging Face";
    public string Description => "The Home of Machine Learning - Create, discover and collaborate on ML better.";
    public string IconUrl => "https://upload.wikimedia.org/wikipedia/he/e/ee/Hugging_Face_logo.png";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new HuggingFaceSettings();
        config.Bind("HuggingFace", settings);
        services.AddSingleton(x =>
        {
            Console.WriteLine($"Loaded HuggingFace settings: {settings.EndPoint} ({settings.Model}) {settings.Token.SubstringMax(4)}");
            return settings;
        });

        services
            .AddRefitClient<IInferenceApi>()
            .AddHttpMessageHandler<AuthHeaderHandler>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(settings.EndPoint));

        services.AddTransient<AuthHeaderHandler>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
    }
}
