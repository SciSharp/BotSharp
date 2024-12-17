using BotSharp.Core.Infrastructures;
using Microsoft.AspNetCore.StaticFiles;

namespace BotSharp.Plugin.AudioHandler.Functions;

public class HandleAudioRequestFn : IFunctionCallback
{
    public string Name => "util-audio-handle_audio_request";
    public string Indication => "Handling audio request";

    private readonly IServiceProvider _serviceProvider;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<HandleAudioRequestFn> _logger;
    private readonly BotSharpOptions _options;

    private readonly IEnumerable<string> _audioContentType = new List<string>
    {
        AudioType.mp3.ToFileType(),
        AudioType.wav.ToFileType(),
    };

    public HandleAudioRequestFn(
        IFileStorageService fileStorage,
        IServiceProvider serviceProvider,
        ILogger<HandleAudioRequestFn> logger,
        BotSharpOptions options)
    {
        _fileStorage = fileStorage;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs, _options.JsonSerializerOptions);
        var conv = _serviceProvider.GetRequiredService<IConversationService>();

        var wholeDialogs = conv.GetDialogHistory();
        var dialogs = AssembleFiles(conv.ConversationId, wholeDialogs);

        var response = await GetResponeFromDialogs(dialogs);
        message.Content = response;
        return true;
    }

    private List<RoleDialogModel> AssembleFiles(string convId, List<RoleDialogModel> dialogs)
    {
        if (dialogs.IsNullOrEmpty())
        {
            return new List<RoleDialogModel>();
        }

        var messageId = dialogs.Select(x => x.MessageId).Distinct().ToList();
        var audioMessageFiles = _fileStorage.GetMessageFiles(convId, messageId, FileSourceType.User, _audioContentType);

        audioMessageFiles = audioMessageFiles.Where(x => x.ContentType.Contains("audio")).ToList();

        foreach (var dialog in dialogs)
        {
            var found = audioMessageFiles.Where(x => x.MessageId == dialog.MessageId).ToList();
            if (found.IsNullOrEmpty()) continue;

            dialog.Files = found.Select(x => new BotSharpFile
            {
                ContentType = x.ContentType,
                FileUrl = x.FileUrl,
                FileStorageUrl = x.FileStorageUrl
            }).ToList();
        }

        return dialogs;
    }

    private async Task<string> GetResponeFromDialogs(List<RoleDialogModel> dialogs)
    {
        var audioCompletion = PrepareModel();
        var dialog = dialogs.Where(x => !x.Files.IsNullOrEmpty()).Last();
        var transcripts = new List<string>();

        foreach (var file in dialog.Files)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.FileStorageUrl)) continue;

            var extension = Path.GetExtension(file.FileStorageUrl);

            var fileName = Path.GetFileName(file.FileStorageUrl);
            if (!ParseAudioFileType(fileName)) continue;

            var bytes = _fileStorage.GetFileBytes(file.FileStorageUrl);
            using var stream = new MemoryStream(bytes);
            stream.Position = 0;

            var result = await audioCompletion.GenerateTextFromAudioAsync(stream, fileName);
            transcripts.Add(result);
            stream.Close();
        }

        if (transcripts.IsNullOrEmpty())
        {
            throw new FileNotFoundException($"No audio files found in the dialog. MessageId: {dialog.MessageId}");
        }

        return string.Join("\r\n\r\n", transcripts);
    }

    private IAudioCompletion PrepareModel()
    {
        return CompletionProvider.GetAudioCompletion(_serviceProvider, provider: "openai", model: "whisper-1");
    }

    private bool ParseAudioFileType(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLower();
        var provider = new FileExtensionContentTypeProvider();
        bool canParse = Enum.TryParse<AudioType>(extension, out _) || provider.TryGetContentType(fileName, out _);
        return canParse;
    }
}
