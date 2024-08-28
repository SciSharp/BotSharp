using OpenAI.Audio;

namespace BotSharp.Plugin.OpenAI.Providers.Audio;

public partial class AudioCompletionProvider
{
    public async Task<string> GenerateTextFromAudioAsync(Stream audio, string audioFileName, string? text = null)
    {
        var audioClient = ProviderHelper.GetClient(Provider, _model, _services)
                                        .GetAudioClient(_model);

        var options = PrepareTranscriptionOptions(text);
        var result = await audioClient.TranscribeAudioAsync(audio, audioFileName, options);
        return result.Value.Text;
    }

    private AudioTranscriptionOptions PrepareTranscriptionOptions(string? text)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var format = GetTranscriptionResponseFormat(state.GetState("audio_response_format"));
        var granularity = GetGranularity(state.GetState("audio_granularity"));
        var temperature = GetTemperature(state.GetState("audio_temperature"));

        var options = new AudioTranscriptionOptions
        {
            ResponseFormat = format,
            Granularities = granularity,
            Temperature = temperature,
            Prompt = text
        };

        return options;
    }

    private AudioTranscriptionFormat GetTranscriptionResponseFormat(string input)
    {
        var value = !string.IsNullOrEmpty(input) ? input : "verbose";

        AudioTranscriptionFormat format;
        switch (value)
        {
            case "json":
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

    private float GetTemperature(string input)
    {
        if (!float.TryParse(input, out var temperature))
        {
            return 0.0f;
        }

        return temperature;
    }
}
