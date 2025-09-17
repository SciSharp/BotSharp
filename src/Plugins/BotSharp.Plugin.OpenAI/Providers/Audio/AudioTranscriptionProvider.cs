using OpenAI.Audio;

namespace BotSharp.Plugin.OpenAI.Providers.Audio;

public class AudioTranscriptionProvider : IAudioTranscription
{
    private readonly IServiceProvider _services;

    public string Provider => "openai";
    public string Model => _model;

    private string _model;

    public AudioTranscriptionProvider(IServiceProvider service)
    {
        _services = service;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    public async Task<string> TranscriptTextAsync(Stream audio, string audioFileName, string? text = null)
    {
        var audioClient = ProviderHelper.GetClient(Provider, _model, _services)
                                        .GetAudioClient(_model);

        var options = PrepareTranscriptionOptions(text);
        var result = await audioClient.TranscribeAudioAsync(audio, audioFileName, options);
        return result.Value.Text;
    }

    private AudioTranscriptionOptions PrepareTranscriptionOptions(string? text)
    {
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var state = _services.GetRequiredService<IConversationStateService>();

        var settings = settingsService.GetSetting(Provider, _model)?.Audio?.Transcription;

        var temperature = state.GetState("audio_temperature");
        var responseFormat = state.GetState("audio_response_format");
        var granularity = state.GetState("audio_granularity");

        if (string.IsNullOrEmpty(temperature) && settings?.Temperature != null)
        {
            temperature = $"{settings.Temperature}";
        }
        
        responseFormat = settings?.ResponseFormat != null ? VerifyTranscriptionParameter(responseFormat, settings.ResponseFormat.Default, settings.ResponseFormat.Options) : null;
        granularity = settings?.Granularity != null ? VerifyTranscriptionParameter(granularity, settings.Granularity.Default, settings.Granularity.Options) : null;

        var options = new AudioTranscriptionOptions
        {
            Prompt = text
        };

        if (!string.IsNullOrEmpty(temperature))
        {
            options.Temperature = GetTemperature(temperature);
        }
        if (!string.IsNullOrEmpty(responseFormat))
        {
            options.ResponseFormat = GetTranscriptionResponseFormat(responseFormat);
        }
        if (!string.IsNullOrEmpty(granularity))
        {
            options.TimestampGranularities = GetGranularity(granularity);
        }

        return options;
    }

    private AudioTranscriptionFormat GetTranscriptionResponseFormat(string input)
    {
        var value = !string.IsNullOrEmpty(input) ? input : "json";

        AudioTranscriptionFormat format;
        switch (value)
        {
            case "json":
                format = new AudioTranscriptionFormat("json");
                break;
            case "text":
                format = new AudioTranscriptionFormat("text");
                break;
            case "simple":
                format = AudioTranscriptionFormat.Simple;
                break;
            case "srt":
                format = AudioTranscriptionFormat.Srt;
                break;
            case "vtt":
                format = AudioTranscriptionFormat.Vtt;
                break;
            default:
                format = AudioTranscriptionFormat.Verbose;
                break;
        }

        return format;
    }

    private AudioTimestampGranularities GetGranularity(string input)
    {
        var value = !string.IsNullOrEmpty(input) ? input : "default";

        AudioTimestampGranularities granularity;
        switch (value)
        {
            case "word":
                granularity = AudioTimestampGranularities.Word;
                break;
            case "segment":
                granularity = AudioTimestampGranularities.Segment;
                break;
            default:
                granularity = AudioTimestampGranularities.Default;
                break;
        }

        return granularity;
    }

    private float? GetTemperature(string input)
    {
        if (!float.TryParse(input, out var temperature))
        {
            return null;
        }

        return temperature;
    }

    private string? VerifyTranscriptionParameter(string? curVal, string? defaultVal, IEnumerable<string>? options = null)
    {
        if (options.IsNullOrEmpty())
        {
            return curVal.IfNullOrEmptyAs(defaultVal);
        }

        return options.Contains(curVal) ? curVal : defaultVal;
    }
}
