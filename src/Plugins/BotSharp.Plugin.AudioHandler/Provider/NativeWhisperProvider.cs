using Whisper.net;
using Whisper.net.Ggml;

namespace BotSharp.Plugin.AudioHandler.Provider;

/// <summary>
/// Native Whisper provider for speech to text conversion
/// </summary>
public class NativeWhisperProvider : ISpeechToText
{
    private readonly IAudioProcessUtilities _audioProcessUtilities;
    private static WhisperProcessor _processor;

    private string _modelName;

    public NativeWhisperProvider(IAudioProcessUtilities audioProcessUtilities)
    {
        _audioProcessUtilities = audioProcessUtilities;
    }

    public async Task<string> AudioToTextTranscript(string filePath)
    {
        string fileExtension = Path.GetExtension(filePath);
        if (!Enum.TryParse<AudioType>(fileExtension.TrimStart('.').ToLower(), out AudioType audioType))
        {
            throw new Exception($"Unsupported audio type: {fileExtension}");
        }
        await InitModel();
        // var _streamHandler = _audioHandlerFactory.CreateAudioHandler(audioType);
        using var stream = _audioProcessUtilities.ConvertToStream(filePath);

        if (stream == null)
        {
            throw new Exception($"Failed to convert {fileExtension} to stream");
        }

        var textResult = new List<SegmentData>();

        await foreach (var result in _processor.ProcessAsync((Stream)stream).ConfigureAwait(false))
        {
            textResult.Add(result);
        }

        var audioOutput = new AudioOutput
        {
            Segments = textResult
        };
        return audioOutput.ToString();
    }
    private async Task LoadWhisperModel(GgmlType modelType)
    {
        try
        {
            _modelName = $"ggml-{modelType}.bin";

            if (!File.Exists(_modelName))
            {
                using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(GgmlType.TinyEn);
                using var fileWriter = File.OpenWrite(_modelName);
                await modelStream.CopyToAsync(fileWriter);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load whisper model: {ex.Message}");
        }
    }

    private async Task InitModel(GgmlType modelType = GgmlType.TinyEn)
    {
        if (_processor == null)
        {

            await LoadWhisperModel(modelType);
            _processor = WhisperFactory
                .FromPath(_modelName)
                .CreateBuilder()
                .WithLanguage("en")
                .Build();
        }
    }
}
