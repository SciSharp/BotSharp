using OpenAI.Audio;
using System.Drawing;

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
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);

        var audioClient = ProviderHelper.GetClient(Provider, _model, apiKey: null, _services)
                                        .GetAudioClient(_model);

        var options = PrepareTranscriptionOptions(text, settings?.Audio?.Transcription);
        var result = await audioClient.TranscribeAudioAsync(audio, audioFileName, options);
        return result.Value.Text;
    }

    private AudioTranscriptionOptions PrepareTranscriptionOptions(string? text, AudioTranscriptionSetting? settings)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var temperature = state.GetState("audio_temperature");
        var responseFormat = state.GetState("audio_response_format");
        var granularity = state.GetState("audio_granularity");

        if (string.IsNullOrEmpty(temperature) && settings?.Temperature != null)
        {
            temperature = $"{settings.Temperature}";
        }

        responseFormat = LlmUtility.GetModelParameter(settings?.Parameters, "ResponseFormat", responseFormat);
        granularity = LlmUtility.GetModelParameter(settings?.Parameters, "Granularity", granularity);

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
}
