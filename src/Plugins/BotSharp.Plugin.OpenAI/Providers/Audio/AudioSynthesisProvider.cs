using BotSharp.Abstraction.Infrastructures.Enums;
using OpenAI.Audio;

namespace BotSharp.Plugin.OpenAI.Providers.Audio;

public class AudioSynthesisProvider : IAudioSynthesis
{
    private readonly IServiceProvider _services;
    public string Provider => "openai";
    public string Model => _model;

    private string _model;

    public AudioSynthesisProvider(IServiceProvider service)
    {
        _services = service;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    // A short phrase normally comes back in a few seconds, but some requests never finish;
    // with the SDK default of 100s per attempt, four stalled attempts block the caller for ~7 minutes.
    private const int DefaultTimeoutSeconds = 30;
    private const int DefaultMaxRetries = 3;

    public async Task<BinaryData> GenerateAudioAsync(string text, string? voice = "alloy", string? format = "mp3", string? instructions = null)
    {
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var synthesis = settingsService.GetSetting(Provider, _model)?.Audio?.Synthesis;
        var timeout = TimeSpan.FromSeconds(synthesis?.TimeoutSeconds ?? DefaultTimeoutSeconds);
        var maxRetries = synthesis?.MaxRetries ?? DefaultMaxRetries;

        var audioClient = ProviderHelper.GetClient(Provider, _model, apiKey: null, _services,
                                            networkTimeout: timeout, maxRetries: maxRetries)
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
        var speed = state.GetState<float?>(AudioStateConst.SPEECH_GENERATE_SPEED);

        var options = new SpeechGenerationOptions
        {
            ResponseFormat = responseFormat,
            SpeedRatio = speed
        };

        return (voice, options);
    }

    private GeneratedSpeechVoice GetVoice(string input)
    {
        GeneratedSpeechVoice voice;
        switch (input)
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
}
