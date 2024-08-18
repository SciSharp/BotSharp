using BotSharp.Core.Agents.Services;
using Whisper.net;
using Whisper.net.Ggml;

namespace BotSharp.Plugin.AudioHandler.Provider;

/// <summary>
/// Native Whisper provider for speech to text conversion
/// </summary>
public class NativeWhisperProvider : ISpeechToText
{
    public string Provider => "native";
    private readonly IAudioProcessUtilities _audioProcessUtilities;
    private static WhisperProcessor _processor;
    private readonly ILogger _logger;

    private string MODEL_DIR = "model";
    private string? _currentModelPath;
    private Dictionary<GgmlType, string> _modelPathDict = new Dictionary<GgmlType, string>();
    private GgmlType? _modelType;

    public NativeWhisperProvider(
        IAudioProcessUtilities audioProcessUtilities,
        ILogger<NativeWhisperProvider> logger)
    {
        _audioProcessUtilities = audioProcessUtilities;
        _logger = logger;
    }

    public async Task<string> GenerateTextFromAudioAsync(string filePath)
    {
        string fileExtension = Path.GetExtension(filePath);
        if (!Enum.TryParse<AudioType>(fileExtension.TrimStart('.').ToLower(), out AudioType audioType))
        {
            throw new Exception($"Unsupported audio type: {fileExtension}");
        }

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

        _processor.Dispose();

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
            if (!Directory.Exists(MODEL_DIR))
                Directory.CreateDirectory(MODEL_DIR);

            var availableModelPaths = Directory.GetFiles(MODEL_DIR, "*.bin")
                .ToArray();

            if (!availableModelPaths.Any())
            {
                _currentModelPath = SetModelPath(MODEL_DIR, modelType);
                await DownloadModel(modelType, _currentModelPath);
            }
            else
            {
                var modelFilePath = availableModelPaths.FirstOrDefault(x => Path.GetFileName(x) == $"ggml-{modelType}.bin");
                if (modelFilePath == null)
                {
                    _currentModelPath = SetModelPath(MODEL_DIR, modelType);
                    await DownloadModel(modelType, _currentModelPath);
                }
                else
                {
                    _currentModelPath = modelFilePath;
                }
            }

            _processor = WhisperFactory
             .FromPath(path: _currentModelPath)
             .CreateBuilder()
             .WithLanguage("auto")
             .Build();

            _modelType = modelType;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to load whisper model: {ex.Message}");
        }
    }

    private async Task DownloadModel(GgmlType modelType, string modelDir)
    {
        using var modelStream = await WhisperGgmlDownloader.GetGgmlModelAsync(modelType);
        using var fileWriter = File.OpenWrite(modelDir);
        await modelStream.CopyToAsync(fileWriter);
    }

    private string SetModelPath(string rootPath, GgmlType modelType)
    {
        string currentModelPath = Path.Combine(rootPath, $"ggml-{modelType}.bin");
        return currentModelPath;
    }

    public async Task SetModelName(string modelType)
    {
        if (Enum.TryParse<GgmlType>(modelType, true, out GgmlType ggmlType))
        {
            await LoadWhisperModel(ggmlType);
            return;
        }

        _logger.LogWarning($"Unsupported model type: {modelType}. Use Tiny model instead!");
        await LoadWhisperModel(GgmlType.Tiny);
    }
}
