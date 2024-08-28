using OpenAI.Audio;

namespace BotSharp.Plugin.OpenAI.Providers.Audio;

public partial class AudioCompletionProvider
{
    public async Task<BinaryData> GenerateSpeechFromTextAsync(string text)
    {
        var audioClient = ProviderHelper.GetClient(Provider, _model, _services)
                                        .GetAudioClient(_model);

        var result = await audioClient.GenerateSpeechFromTextAsync(text, GeneratedSpeechVoice.Alloy);
        return result.Value;
    }

    private SpeechGenerationOptions PrepareGenerationOptions()
    {
        return new SpeechGenerationOptions
        {

        };
    }
}
