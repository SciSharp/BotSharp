namespace BotSharp.Plugin.AudioHandler.Functions;

public class ReadAudioFn : IFunctionCallback
{
    public string Name => "util-audio-handle_audio_request";
    public string Indication => "Reading audio";

    private readonly IServiceProvider _services;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<ReadAudioFn> _logger;
    private readonly BotSharpOptions _options;
    private readonly AudioHandlerSettings _settings;

    private readonly IEnumerable<string> _audioContentTypes = new List<string>
    {
        AudioType.mp3.ToFileType(),
        AudioType.wav.ToFileType(),
    };

    public ReadAudioFn(
        IServiceProvider services,
        ILogger<ReadAudioFn> logger,
        BotSharpOptions options,
        AudioHandlerSettings settings,
        IFileStorageService fileStorage)
    {
        _services = services;
        _logger = logger;
        _options = options;
        _settings = settings;
        _fileStorage = fileStorage;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs, _options.JsonSerializerOptions);
        var conv = _services.GetRequiredService<IConversationService>();
        var routingCtx = _services.GetRequiredService<IRoutingContext>();

        var wholeDialogs = routingCtx.GetDialogs();
        if (wholeDialogs.IsNullOrEmpty())
        {
            wholeDialogs = conv.GetDialogHistory();
        }

        var dialogs = AssembleFiles(conv.ConversationId, wholeDialogs);
        var response = await GetAudioTranscription(dialogs);
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

    private async Task<string> GetAudioTranscription(List<RoleDialogModel> dialogs)
    {
        var audioCompletion = PrepareModel();
        var dialog = dialogs.Where(x => !x.Files.IsNullOrEmpty()).LastOrDefault();
        var transcripts = new List<string>();

        if (dialog != null)
        {
            foreach (var file in dialog.Files)
            {
                if (string.IsNullOrWhiteSpace(file?.FileStorageUrl))
                {
                    continue;
                }

                var extension = Path.GetExtension(file.FileStorageUrl);
                var fileName = Path.GetFileName(file.FileStorageUrl);
                if (!VerifyAudioFileType(fileName))
                {
                    continue;
                }

                var binary = _fileStorage.GetFileBytes(file.FileStorageUrl);
                using var stream = binary.ToStream();
                stream.Position = 0;

                var result = await audioCompletion.TranscriptTextAsync(stream, fileName);
                transcripts.Add(result);
                stream.Close();
                await Task.Delay(100);
            }
        }
        

        if (transcripts.IsNullOrEmpty())
        {
            var msg = "No audio is found in the chat.";
            _logger.LogWarning(msg);
            transcripts.Add(msg);
        }

        return string.Join("\r\n\r\n", transcripts);
    }

    private IAudioTranscription PrepareModel()
    {
        var (provider, model) = GetLlmProviderModel();
        return CompletionProvider.GetAudioTranscriber(_services, provider: provider, model: model);
    }

    private bool VerifyAudioFileType(string fileName)
    {
        var extension = Path.GetExtension(fileName).TrimStart('.').ToLower();
        return Enum.TryParse<AudioType>(extension, out _)
                    || !string.IsNullOrEmpty(FileUtility.GetFileContentType(fileName));
    }

    private (string, string) GetLlmProviderModel()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var llmProviderService = _services.GetRequiredService<ILlmProviderService>();

        var provider = state.GetState("audio_read_llm_provider");
        var model = state.GetState("audio_read_llm_provider");

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = _settings?.Audio?.Reading?.LlmProvider;
        model = _settings?.Audio?.Reading?.LlmModel;

        if (!string.IsNullOrEmpty(provider) && !string.IsNullOrEmpty(model))
        {
            return (provider, model);
        }

        provider = "openai";
        model = "gpt-4o-mini-transcribe";

        return (provider, model);
    }
}
