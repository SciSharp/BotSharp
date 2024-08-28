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
        var options = new AudioTranscriptionOptions
        {
            ResponseFormat = AudioTranscriptionFormat.Verbose,
            Granularities = AudioTimestampGranularities.Word | AudioTimestampGranularities.Segment,
            Prompt = text
        };

        return options;
    }

    private AudioTranscriptionFormat GetTranscriptionResponseFormat(string format)
    {
        var value = !string.IsNullOrEmpty(format) ? format : "verbose";

        AudioTranscriptionFormat retFormat;
        switch (value)
        {
            case "json":
                retFormat = AudioTranscriptionFormat.Simple;
                break;
            case "srt":
                retFormat = AudioTranscriptionFormat.Srt;
                break;
            case "vtt":
                retFormat = AudioTranscriptionFormat.Vtt;
                break;
            default:
                retFormat = AudioTranscriptionFormat.Verbose;
                break;
        }

        return retFormat;
    }
}
