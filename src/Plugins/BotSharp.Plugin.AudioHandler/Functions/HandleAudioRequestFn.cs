using BotSharp.Abstraction.Agents.Models;
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
    private Agent? _agent;

    private readonly IEnumerable<string> _audioContentType = new List<string>
    {
        AudioType.mp3.ToFileType(),
        AudioType.wav.ToFileType(),
    };


    public HandleAudioRequestFn(
        IServiceProvider serviceProvider,
        ILogger<HandleAudioRequestFn> logger,
        BotSharpOptions options
        )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs, _options.JsonSerializerOptions);
        var conv = _serviceProvider.GetRequiredService<IConversationService>();
        var isNeedSummary = args?.IsNeedSummary ?? false;

        var wholeDialogs = conv.GetDialogHistory();
        var dialogs = await AssembleFiles(conv.ConversationId, wholeDialogs);

        var response = await GetResponeFromDialogs(dialogs); // isNeedSummary ? await SummarizeAudioText : TranscribeAudioToText;
        message.Content = response;
        return true;

        //for ( int i = 0; i <dialogs.Count; i++)
        //{
        //    var dialog = dialogs[i];
        //    var updatedFiles = new List<BotSharpFile>();
        //    foreach (var file in dialog.Files) 
        //    {
        //        if (Enum.TryParse<AudioType>(file.ContentType.ToLower(), out var fileType) 
        //            && !string.IsNullOrWhiteSpace(file?.FileUrl))
        //        {
        //            var transcribeText = await nativeWhisperService.AudioToTextTranscript(file?.FileUrl);

        //            if (isNeedSummary) 
        //            {
        //                var fileAgent = new Agent
        //                {
        //                    Id = _agent?.Id ?? Guid.Empty.ToString(),
        //                    Name = _agent?.Name ?? "Unkown",
        //                    Instruction = !string.IsNullOrWhiteSpace(args?.UserRequest) ? args.UserRequest : "Please summarize the those content.",
        //                    TemplateDict = new Dictionary<string, object>()
        //                };
        //                var response = await GetChatCompletion(fileAgent, dialogs);

        //            }
        //        }
        //        updatedFiles.Add(file);
        //    }
        //    dialog.Files = updatedFiles;
        //}

        //throw new NotImplementedException();
    }

    private async Task<List<RoleDialogModel>> AssembleFiles(string convId, List<RoleDialogModel> dialogs)
    {
        if (dialogs.IsNullOrEmpty())
            return new List<RoleDialogModel>();

        var fileService = _serviceProvider.GetRequiredService<IFileStorageService>();
        var messageId = dialogs.Select(x => x.MessageId).Distinct().ToList();
        var audioMessageFiles = fileService.GetMessageFiles(convId, messageId, FileSourceType.User, _audioContentType);

        audioMessageFiles = audioMessageFiles.Where(x => x.ContentType.Contains("audio")).ToList();

        foreach (var dialog in dialogs)
        {
            var found = audioMessageFiles.Where(x => x.MessageId == dialog.MessageId).ToList();
            if (found.IsNullOrEmpty())
                continue;

            dialog.Files = found.Select(x => new BotSharpFile
            {
                ContentType = x.ContentType,
                FileUrl = x.FileUrl,
                FileStorageUrl = x.FileStorageUrl
            }).ToList();
        }

        return dialogs;
    }

    private bool ParseAudioFileType(string fileType)
    {
        fileType = fileType.ToLower();
        var provider = new FileExtensionContentTypeProvider();
        bool canParse = Enum.TryParse<AudioType>(fileType, out var fileEnumType) || provider.TryGetContentType(fileType, out string contentType);
        return canParse;
    }

    private async Task<string> GetResponeFromDialogs(List<RoleDialogModel> dialogs)
    {
        var whisperService = PrepareModel("openai");
        var dialog = dialogs.Where(x => !x.Files.IsNullOrEmpty()).Last();
        int transcribedCount = 0;
        foreach (var file in dialog.Files)
        {
            if (file == null)
                continue;

            string extension = Path.GetExtension(file?.FileStorageUrl);
            if (ParseAudioFileType(extension) && File.Exists(file.FileStorageUrl))
            {
                file.FileData = await whisperService.GenerateTextFromAudioAsync(file.FileStorageUrl);
                transcribedCount++;
            }
        }

        if (transcribedCount == 0)
        {
            throw new FileNotFoundException($"No audio files found in the dialog. MessageId: {dialog.MessageId}");
        }

        //if (isNeedSummary) 
        //{
        //    await LoadAgent();

        //    var llmProviderService = _serviceProvider.GetRequiredService<ILlmProviderService>();
        //    var provider = llmProviderService.GetProviders().FirstOrDefault(x => x == "openai");
        //    var model = llmProviderService.GetProviderModel(provider: provider, id: "gpt-4", multiModal: false);
        //    var completion = CompletionProvider.GetChatCompletion(_serviceProvider, provider: provider, model: model.Name);

        //    var response = await completion.GetChatCompletions(_agent, new List<RoleDialogModel> { dialog });
        //}

        //PostProcessingText();
        var resList = dialog.Files.Select(x => $"{x.FileName} \r\n {x.FileData}").ToList();
        return string.Join("\n\r", resList);
    }

    private ISpeechToText PrepareModel(string modelName = "native")
    {
        var whisperService = _serviceProvider.GetServices<ISpeechToText>().FirstOrDefault(x => x.Provider == modelName.ToLower());
        if (whisperService == null)
        {
            throw new Exception($"Can't resolve speech2text provider by {modelName}");
        }

        if (modelName.Equals("openai", StringComparison.OrdinalIgnoreCase))
        {
            return CompletionProvider.GetSpeechToText(_serviceProvider, provider: "openai", model: "whisper-1");
        }
        return whisperService;
    }

    /*
    private async Task LoadAgent()
    {
        if (_agent == null)
        {
            var agentService = _serviceProvider.GetRequiredService<IAgentService>();
            var agent = await agentService.LoadAgent(BuiltInAgentId.UtilityAssistant);

            var fileAgent = new Agent
            {
                Id = agent?.Id ?? Guid.Empty.ToString(),
                Name = agent?.Name ?? "Unkown",
                Instruction = !string.IsNullOrWhiteSpace(args?.UserRequest) ? args.UserRequest : "Please generate a short summary based on the text. \r\n",
                TemplateDict = new Dictionary<string, object>()
            };
            _agent = fileAgent;
        }
    }
    */

    //private async Task<string> GetChatCompletion(Agent agent, List<RoleDialogModel> roleDialogs)
    //{
    //    try
    //    {
    //        var llmProviderService = _serviceProvider.GetRequiredService<ILlmProviderService>();
    //        var provider = llmProviderService.GetProviders().FirstOrDefault(x => x == "openai");
    //        var model = llmProviderService.GetProviderModel(provider: provider, id: "gpt-4", multiModal: false);
    //        var completion = CompletionProvider.GetChatCompletion(_serviceProvider, provider: provider, model: model.Name);
    //        var response = await completion.GetChatCompletions(agent, dialogs);


    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogWarning($"Error when summarizing the audio text. {ex.Message}\r\n{ex.InnerException}");
    //        return string.Empty;
    //    }
    //}
}
