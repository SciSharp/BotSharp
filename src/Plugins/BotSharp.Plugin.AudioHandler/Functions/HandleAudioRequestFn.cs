using BotSharp.Abstraction.Routing;
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

    private readonly IEnumerable<string> _audioContentTypes = new List<string>
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
        var routingCtx = _serviceProvider.GetRequiredService<IRoutingContext>();

        var wholeDialogs = routingCtx.GetDialogs();
        if (wholeDialogs.IsNullOrEmpty())
        {
            wholeDialogs = conv.GetDialogHistory();
        }

        var dialogs = AssembleFiles(conv.ConversationId, wholeDialogs);
        var response = await GetResponeFromDialogs(dialogs);
        message.Content = response;
        dialogs.ForEach(x => x.Files = null);
        return true;
    }

    private List<RoleDialogModel> AssembleFiles(string convId, List<RoleDialogModel> dialogs)
    {
        if (dialogs.IsNullOrEmpty())
        {
            return new List<RoleDialogModel>();
        }

        var messageId = dialogs.Select(x => x.MessageId).Distinct().ToList();
        var audioFiles = _fileStorage.GetMessageFiles(convId, messageId, options: new()
        {
            Sources = [FileSource.User],
            ContentTypes = _audioContentTypes
        });

        audioFiles = audioFiles.Where(x => x.ContentType.Contains("audio")).ToList();

        foreach (var dialog in dialogs)
        {
            var found = audioFiles.Where(x => x.MessageId == dialog.MessageId
                                           && x.FileSource.IsEqualTo(FileSource.User)).ToList();

            if (found.IsNullOrEmpty() || !dialog.IsFromUser)
            {
                continue;
            }

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

            var binary = _fileStorage.GetFileBytes(file.FileStorageUrl);
            using var stream = binary.ToStream();
            stream.Position = 0;

            var result = await audioCompletion.TranscriptTextAsync(stream, fileName);
            transcripts.Add(result);
            stream.Close();
        }

        if (transcripts.IsNullOrEmpty())
        {
            throw new FileNotFoundException($"No audio files found in the dialog. MessageId: {dialog.MessageId}");
        }

        return string.Join("\r\n\r\n", transcripts);
    }

    private IAudioTranscription PrepareModel()
    {
        return CompletionProvider.GetAudioTranscriber(_serviceProvider);
    }

    private bool ParseAudioFileType(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLower();
        var provider = new FileExtensionContentTypeProvider();
        bool canParse = Enum.TryParse<AudioType>(extension, out _) || provider.TryGetContentType(fileName, out _);
        return canParse;
    }
}
