using BotSharp.Abstraction.Functions;
using System.Net.Http;

namespace BotSharp.Core.Files.Functions;

public class GenerateImageFn : IFunctionCallback
{
    public string Name => "generate_image";
    public string Indication => "Generating image";

    private readonly IServiceProvider _services;
    private readonly ILogger<GenerateImageFn> _logger;
    private static string UTILITY_ASSISTANT = Guid.Empty.ToString();
    private string _conversationId;
    private string _messageId;

    public GenerateImageFn(
        IServiceProvider services,
        ILogger<GenerateImageFn> logger)
    {
        _services = services;
        _logger = logger;
    }


    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmFileContext>(message.FunctionArgs);
        Init(message);
        SetImageOptions();
        
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(UTILITY_ASSISTANT);
        var imageAgent = new Agent
        {
            Id = agent?.Id ?? Guid.Empty.ToString(),
            Name = agent?.Name ?? "Unkown",
            Instruction = args?.ImageDescription,
            TemplateDict = new Dictionary<string, object>()
        };

        var response = await GetImageGeneration(imageAgent, message, args?.ImageDescription);
        message.Content = response;
        message.StopCompletion = true;
        return true;
    }

    private void Init(RoleDialogModel message)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        _conversationId = convService.ConversationId;
        _messageId = message.MessageId;
    }

    private void SetImageOptions()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var size = state.SetState("image_size", "1024x1024");
        var quality = state.SetState("image_quality", "standard");
        var style = state.SetState("image_style", "natural");
        var format = state.SetState("image_format", "bytes");
        var count = state.SetState("image_count", "1");
    }

    private async Task<string> GetImageGeneration(Agent agent, RoleDialogModel message, string? description)
    {
        try
        {
            var completion = CompletionProvider.GetImageGeneration(_services, provider: "openai", model: "dall-e-3", imageGenerate: true);
            var text = !string.IsNullOrWhiteSpace(description) ? description : message.Content;
            var dialog = RoleDialogModel.From(message, AgentRole.User, text);
            var result = await completion.GetImageGeneration(agent, new List<RoleDialogModel> { dialog });
            await SaveGeneratedImages(result?.GeneratedImages);
            return result?.Content ?? string.Empty;
        }
        catch (Exception ex)
        {
            var error = $"Error when generating image.";
            _logger.LogWarning($"{error} {ex.Message}");
            return error;
        }
    }

    private async Task SaveGeneratedImages(List<ImageGeneration>? images)
    {
        if (images.IsNullOrEmpty()) return;

        var files = new List<BotSharpFile>();
        foreach (var image in images)
        {
            if (string.IsNullOrEmpty(image?.ImageUrl)
                && string.IsNullOrEmpty(image?.ImageData))
            {
                continue;
            }

            try
            {
                var data = image.ImageData;
                if (!string.IsNullOrEmpty(image.ImageUrl))
                {
                    var http = _services.GetRequiredService<IHttpClientFactory>();
                    using var client = http.CreateClient();
                    var bytes = await client.GetByteArrayAsync(image.ImageUrl);
                    data = Convert.ToBase64String(bytes);
                }

                if (!string.IsNullOrEmpty(data))
                {
                    var imageName = $"{Guid.NewGuid().ToString()}.png";
                    var imageData = $"data:image/png;base64,{data}";
                    files.Add(new BotSharpFile { FileName = imageName, FileData = imageData });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error when saving generated image: {image.ImageUrl ?? image.ImageData}\r\n{ex.Message}");
                continue;
            }
        }

        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        fileService.SaveMessageFiles(_conversationId, _messageId, FileSourceType.Bot, files);
    }
}
