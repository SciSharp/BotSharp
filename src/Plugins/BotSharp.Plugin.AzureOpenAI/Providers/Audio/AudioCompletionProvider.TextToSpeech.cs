using OpenAI.Audio;

namespace BotSharp.Plugin.AzureOpenAI.Providers.Audio;

public partial class AudioCompletionProvider
{
    public async Task<BinaryData> GenerateAudioFromTextAsync(string text, string? voice = "alloy", string? format = "mp3")
    {
        var audioClient = ProviderHelper.GetClient(Provider, _model, _services)
                                        .GetAudioClient(_model);

        var (speechVoice, options) = PrepareGenerationOptions(voice: voice, format: format);
        var result = await audioClient.GenerateSpeechAsync(text, speechVoice, options);
        return result.Value;
    }

    private (GeneratedSpeechVoice, SpeechGenerationOptions) PrepareGenerationOptions(string? voice, string? format)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var speechVoice = GetVoice(voice ?? "alloy");
        var responseFormat = GetSpeechFormat(format ?? "mp3");
        var speed = GetSpeed(state.GetState("speech_generate_speed"));

        var options = new SpeechGenerationOptions
        {
            ResponseFormat = responseFormat,
            SpeedRatio = speed,
        };

        return (voice, options);
    }

    private GeneratedSpeechVoice GetVoice(string input)
    {
        var value = !string.IsNullOrEmpty(input) ? input : "alloy";

        GeneratedSpeechVoice voice;
        switch (value)
        {
            case "echo":
                voice = GeneratedSpeechVoice.Echo;
                break;
            case "fable":
                voice = GeneratedSpeechVoice.Fable;
                break;
            case "onyx":
                voice = GeneratedSpeechVoice.Onyx;
                break;
            case "nova":
                voice = GeneratedSpeechVoice.Nova;
                break;
            case "shimmer":
                voice = GeneratedSpeechVoice.Shimmer;
                break;
            default:
                voice = GeneratedSpeechVoice.Alloy;
                break;
        }

        return voice;
    }

    private GeneratedSpeechFormat GetSpeechFormat(string input)
    {
        var value = !string.IsNullOrEmpty(input) ? input : "mp3";

        GeneratedSpeechFormat format;
        switch (value)
        {
            case "wav":
                format = GeneratedSpeechFormat.Wav;
                break;
            case "opus":
                format = GeneratedSpeechFormat.Opus;
                break;
            case "aac":
                format = GeneratedSpeechFormat.Aac;
                break;
            case "flac":
                format = GeneratedSpeechFormat.Flac;
                break;
            case "pcm":
                format = GeneratedSpeechFormat.Pcm;
                break;
            default:
                format = GeneratedSpeechFormat.Mp3;
                break;
        }

        return format;
    }

    private float? GetSpeed(string input)
    {
        if (!float.TryParse(input, out var speed))
        {
            return null;
        }

        return speed;
    }
}
