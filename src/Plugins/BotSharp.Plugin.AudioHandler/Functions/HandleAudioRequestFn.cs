using BotSharp.Core.Infrastructures;
using Microsoft.AspNetCore.StaticFiles;

namespace BotSharp.Plugin.AudioHandler.Functions;

public class HandleAudioRequestFn : IFunctionCallback
{
    public string Name => "handle_audio_request";
    public string Indication => "Handling audio request";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HandleAudioRequestFn> _logger;
    private readonly BotSharpOptions _options;

    private readonly IEnumerable<string> _audioContentType = new List<string>
    {
        AudioType.mp3.ToFileType(),
        AudioType.wav.ToFileType(),
    };

    public HandleAudioRequestFn(
        IServiceProvider serviceProvider,
        ILogger<HandleAudioRequestFn> logger,
        BotSharpOptions options)
    {
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
        if (dialogs.IsNullOrEmpty()) return new List<RoleDialogModel>();

        var fileService = _serviceProvider.GetRequiredService<IFileStorageService>();
        var messageId = dialogs.Select(x => x.MessageId).Distinct().ToList();
        var audioMessageFiles = fileService.GetMessageFiles(convId, messageId, FileSourceType.User, _audioContentType);

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
        var speech2Text = await PrepareModel("native");
        var dialog = dialogs.Where(x => !x.Files.IsNullOrEmpty()).Last();
        int transcribedCount = 0;

        foreach (var file in dialog.Files)
        {
            if (file == null) continue;

            string extension = Path.GetExtension(file?.FileStorageUrl);
            if (ParseAudioFileType(extension) && File.Exists(file.FileStorageUrl))
            {
                file.FileData = await speech2Text.GenerateTextFromAudioAsync(file.FileStorageUrl);
                transcribedCount++;
            }
        }

        if (transcribedCount == 0)
        {
            throw new FileNotFoundException($"No audio files found in the dialog. MessageId: {dialog.MessageId}");
        }

        var resList = dialog.Files.Select(x => $"{x.FileName} \r\n {x.FileData}").ToList();
        return string.Join("\n\r", resList);
    }

    private async Task<ISpeechToText> PrepareModel(string provider = "native")
    {
        var speech2Text = _serviceProvider.GetServices<ISpeechToText>().FirstOrDefault(x => x.Provider == provider.ToLower());
        if (speech2Text == null)
        {
            throw new Exception($"Can't resolve speech2text provider by {provider}");
        }

        if (provider.IsEqualTo("openai"))
        {
            return CompletionProvider.GetSpeechToText(_serviceProvider, provider: "openai", model: "whisper-1");
        }

        await speech2Text.SetModelName("Tiny");
        return speech2Text;
    }

    private bool ParseAudioFileType(string fileType)
    {
        fileType = fileType.ToLower();
        var provider = new FileExtensionContentTypeProvider();
        bool canParse = Enum.TryParse<AudioType>(fileType, out _) || provider.TryGetContentType(fileType, out _);
        return canParse;
    }
}
