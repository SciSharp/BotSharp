using Whisper.net;
using Whisper.net.Ggml;

namespace BotSharp.Plugin.AudioHandler.Provider;

/// <summary>
/// Native Whisper provider for speech to text conversion
/// </summary>
public class NativeWhisperProvider : ISpeechToText
{
    private readonly IAudioHelper _audioProcessor;
    private static WhisperProcessor _whisperProcessor;
    private readonly ILogger<NativeWhisperProvider> _logger;

    public string Provider => "native";

    private string MODEL_DIR = "model";
    private string? _currentModelPath;

    private Dictionary<GgmlType, string> _modelPathDict = new Dictionary<GgmlType, string>();
    private GgmlType? _modelType;

    public NativeWhisperProvider(
        IAudioHelper audioProcessor,
        ILogger<NativeWhisperProvider> logger)
    {
        _audioProcessor = audioProcessor;
        _logger = logger;
    }

    public async Task<string> GenerateTextFromAudioAsync(string filePath)
    {
        string fileExtension = Path.GetExtension(filePath);
        if (!Enum.TryParse(fileExtension.TrimStart('.').ToLower(), out AudioType audioType))
        {
            throw new Exception($"Unsupported audio type: {fileExtension}");
        }

        using var stream = _audioProcessor.ConvertToStream(filePath);
        if (stream == null)
        {
            throw new Exception($"Failed to convert {fileExtension} to stream");
        }

        var textResult = new List<SegmentData>();
        await foreach (var result in _whisperProcessor.ProcessAsync(stream).ConfigureAwait(false))
        {
            textResult.Add(result);
        }

        _whisperProcessor.Dispose();

        var audioOutput = new AudioOutput
        {
            Segments = textResult
        };
        return audioOutput.ToString();
    }

    public Task<string> GenerateTextFromAudioAsync(Stream audio, string audioFileName)
    {
        throw new NotImplementedException();
    }

    public async Task SetModelName(string model)
    {
        if (Enum.TryParse(model, true, out GgmlType ggmlType))
        {
            await LoadWhisperModel(ggmlType);
            return;
        }

        _logger.LogWarning($"Unsupported model type: {model}. Use Tiny model instead!");
        await LoadWhisperModel(GgmlType.Tiny);
    }

    private async Task LoadWhisperModel(GgmlType modelType)
    {
        try
        {
            if (!Directory.Exists(MODEL_DIR))
            {
                Directory.CreateDirectory(MODEL_DIR);
            }

            var availableModelPaths = Directory.GetFiles(MODEL_DIR, "*.bin").ToArray();
            if (availableModelPaths.IsNullOrEmpty())
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

            _whisperProcessor = WhisperFactory.FromPath(path: _currentModelPath).CreateBuilder().WithLanguage("auto").Build();
            _modelType = modelType;
        }
        catch (Exception ex)
        {
            var error = "Failed to load whisper model";
            _logger.LogWarning($"${error}: {ex.Message}\r\n{ex.InnerException}");
            throw new Exception($"{error}: {ex.Message}");
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
}
