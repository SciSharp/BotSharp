using Whisper.net;
using Whisper.net.Ggml;

namespace BotSharp.Plugin.AudioHandler.Provider;

/// <summary>
/// Native Whisper provider for speech to text conversion
/// </summary>
public class NativeWhisperProvider : IAudioCompletion
{
    private static WhisperProcessor _whisperProcessor;

    private readonly IServiceProvider _services;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<NativeWhisperProvider> _logger;

    public string Provider => "native";

    public NativeWhisperProvider(
        BotSharpDatabaseSettings dbSettings,
        IFileStorageService fileStorage,
        IServiceProvider services,
        ILogger<NativeWhisperProvider> logger)
    {
        _fileStorage = fileStorage;
        _services = services;
        _logger = logger;
    }

    public async Task<string> GenerateTextFromAudioAsync(Stream audio, string audioFileName, string? text = null)
    {
        var textResult = new List<SegmentData>();

        using var stream = AudioHelper.Transform(audio, audioFileName);
        await foreach (var result in _whisperProcessor.ProcessAsync(stream).ConfigureAwait(false))
        {
            textResult.Add(result);
        }

        _whisperProcessor.Dispose();
        stream.Close();

        var audioOutput = new AudioOutput
        {
            Segments = textResult
        };
        return audioOutput.ToString();
    }

    public async Task<BinaryData> GenerateSpeechFromTextAsync(string text)
    {
        throw new NotImplementedException();
    }

    public void SetModelName(string model)
    {
        if (Enum.TryParse(model, true, out GgmlType ggmlType))
        {
            LoadWhisperModel(ggmlType);
        }
        else
        {
            _logger.LogWarning($"Unsupported model type: {model}. Use Tiny model instead!");
            LoadWhisperModel(GgmlType.Tiny);
        }
    }

    private void LoadWhisperModel(GgmlType modelType)
    {
        try
        {
            var modelDir = _fileStorage.BuildDirectory("models", "whisper");
            var exist = _fileStorage.ExistDirectory(modelDir);
            if (!exist)
            {
                _fileStorage.CreateDirectory(modelDir);
            }

            var files = _fileStorage.GetFiles("models/whisper", "*.bin");
            var modelLoc = files.FirstOrDefault(x => Path.GetFileName(x) == BuildModelFile(modelType));
            if (string.IsNullOrEmpty(modelLoc))
            {
                modelLoc = BuildModelPath(modelType);
                DownloadModel(modelType, modelLoc);
            }

            var bytes = _fileStorage.GetFileBytes(modelLoc);
            _whisperProcessor = WhisperFactory.FromBuffer(buffer: bytes).CreateBuilder().WithLanguage("auto").Build();
        }
        catch (Exception ex)
        {
            var error = "Failed to load whisper model";
            _logger.LogWarning($"${error}: {ex.Message}\r\n{ex.InnerException}");
            throw new Exception($"{error}: {ex.Message}");
        }
    }

    private void DownloadModel(GgmlType modelType, string modelDir)
    {
        using var modelStream = WhisperGgmlDownloader.GetGgmlModelAsync(modelType).ConfigureAwait(false).GetAwaiter().GetResult();
        _fileStorage.SaveFileStreamToPath(modelDir, modelStream);
        modelStream.Close();
    }

    private string BuildModelPath(GgmlType modelType)
    {
        return _fileStorage.BuildDirectory("models", "whisper", BuildModelFile(modelType));
    }

    private string BuildModelFile(GgmlType modelType)
    {
        return $"ggml-{modelType}.bin";
    }
}
