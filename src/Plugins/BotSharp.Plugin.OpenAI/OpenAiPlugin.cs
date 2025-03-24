using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.OpenAI.Providers.Embedding;
using BotSharp.Plugin.OpenAI.Providers.Image;
using BotSharp.Plugin.OpenAI.Providers.Text;
using BotSharp.Plugin.OpenAI.Providers.Chat;
using BotSharp.Plugin.OpenAI.Providers.Audio;
using Microsoft.Extensions.Configuration;
using Refit;
using BotSharp.Plugin.OpenAI.Providers.Realtime;

namespace BotSharp.Plugin.OpenAI;

/// <summary>
/// OpenAI Service
/// </summary>
public class OpenAiPlugin : IBotSharpPlugin
{
    public string Id => "a743e90f-7cbc-4e47-b8a0-2f8e44f894c7";
    public string Name => "OpenAI";
    public string Description => "OpenAI Service including text generation, text to image and other AI services.";
    public string IconUrl => "https://logosandtypes.com/wp-content/uploads/2022/07/openai.svg";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<OpenAiSettings>("OpenAi");
        });

        services.AddScoped<ITextCompletion, TextCompletionProvider>();
        services.AddScoped<IChatCompletion, ChatCompletionProvider>();
        services.AddScoped<ITextEmbedding, TextEmbeddingProvider>();
        services.AddScoped<IImageCompletion, ImageCompletionProvider>();
        services.AddScoped<IAudioTranscription, AudioTranscriptionProvider>();
        services.AddScoped<IAudioSynthesis, AudioSynthesisProvider>();
        services.AddScoped<IRealTimeCompletion, RealTimeCompletionProvider>();

        services.AddRefitClient<IOpenAiRealtimeApi>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.openai.com"));
    }
}