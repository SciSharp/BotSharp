using OpenAI.Audio;

namespace BotSharp.Plugin.OpenAI.Providers.Audio;

public class SpeechToTextProvider : ISpeechToText
{
    private readonly IServiceProvider _services;

    public string Provider => "openai";
    private string? _model;

    public SpeechToTextProvider(IServiceProvider service)
    {
        _services = service;
    }

    public async Task<string> GenerateTextFromAudioAsync(string filePath)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services)
                                   .GetAudioClient(_model);

        var options = PrepareOptions();
        var result = await client.TranscribeAudioAsync(filePath, options);
        return result.Value.Text;
    }

    public async Task<string> GenerateTextFromAudioAsync(Stream audio, string audioFileName)
    {
        var audioClient = ProviderHelper.GetClient(Provider, _model, _services)
                                        .GetAudioClient(_model);

        var options = PrepareOptions();
        var result = await audioClient.TranscribeAudioAsync(audio, audioFileName, options);
        return result.Value.Text;
    }

    public async Task SetModelName(string model)
    {
        _model = model;
    }

    private AudioTranscriptionOptions PrepareOptions()
    {
        return new AudioTranscriptionOptions
        {
            ResponseFormat = AudioTranscriptionFormat.Verbose,
            Granularities = AudioTimestampGranularities.Word | AudioTimestampGranularities.Segment,
        };
    }
}
