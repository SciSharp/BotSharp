using System.Text;
using OpenAI.Audio;

namespace BotSharp.Plugin.OpenAI.Providers.Audio;

public class SpeechToTextProvider : ISpeechToText
{
    public string Provider => "openai";
    private readonly IServiceProvider _services;
    private string? _modelName;
    private AudioTranscriptionOptions? _options;

    public SpeechToTextProvider(IServiceProvider service)
    {
        _services = service;
    }

    public async Task<string> GenerateTextFromAudioAsync(string filePath)
    {
        var client = ProviderHelper
            .GetClient(Provider, _modelName, _services)
            .GetAudioClient(_modelName);
        SetOptions();
        
        var transcription = await client.TranscribeAudioAsync(filePath);
        
        return transcription.Value.Text;
    }

    public void SetModelName(string modelName)
    {
        if (string.IsNullOrEmpty(_modelName))
        {
            _modelName = modelName;
        }
    }

    public void SetOptions(AudioTranscriptionOptions? options = null)
    {
        if (_options == null)
        {
            _options = options ?? new AudioTranscriptionOptions
            {
                ResponseFormat = AudioTranscriptionFormat.Verbose,
                Granularities = AudioTimestampGranularities.Word | AudioTimestampGranularities.Segment,
            };
        }
    }
}
