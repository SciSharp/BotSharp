using OpenAI.Images;

namespace BotSharp.Plugin.OpenAI.Providers.Image;

public class ImageGenerationProvider : IImageGeneration
{
    protected readonly OpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;

    private const int DEFAULT_IMAGE_COUNT = 1;
    private const int IMAGE_COUNT_LIMIT = 5;

    protected string _model;

    public virtual string Provider => "openai";

    public ImageGenerationProvider(
        OpenAiSettings settings,
        ILogger<ImageGenerationProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _services = services;
        _logger = logger;
    }


    public async Task<RoleDialogModel> GetImageGeneration(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (prompt, imageCount, options) = PrepareOptions(conversations);
        var imageClient = client.GetImageClient(_model);

        var response = imageClient.GenerateImages(prompt, imageCount, options);
        var values = response.Value;

        var images = new List<ImageGeneration>();
        foreach (var value in values)
        {
            if (value == null) continue;

            var image = new ImageGeneration { Description = value?.RevisedPrompt ?? string.Empty };
            if (options.ResponseFormat == GeneratedImageFormat.Uri)
            {
                image.ImageUrl = value?.ImageUri?.AbsoluteUri ?? string.Empty;
            }
            else if (options.ResponseFormat == GeneratedImageFormat.Bytes)
            {
                var base64Str = string.Empty;
                var bytes = value?.ImageBytes?.ToArray();
                if (!bytes.IsNullOrEmpty())
                {
                    base64Str = Convert.ToBase64String(bytes);
                }
                image.ImageData = base64Str;
            }

            images.Add(image);
        }

        var content = string.Join("\r\n", images.Select(x => x.Description));
        var responseMessage = new RoleDialogModel(AgentRole.Assistant, content)
        {
            CurrentAgentId = agent.Id,
            MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
            GeneratedImages = images
        };

        // After
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                PromptCount = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count(),
                CompletionCount = content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Count()
            });
        }

        return responseMessage;
    }

    private (string, int, ImageGenerationOptions) PrepareOptions(List<RoleDialogModel> conversations)
    {
        var prompt = conversations.LastOrDefault()?.Payload ?? conversations.LastOrDefault()?.Content ?? string.Empty;

        var state = _services.GetRequiredService<IConversationStateService>();
        var size = state.GetState("image_size");
        var quality = state.GetState("image_quality");
        var style = state.GetState("image_style");
        var format = state.GetState("image_format");
        var count = GetImageCount(state.GetState("image_count", "1"));

        var options = new ImageGenerationOptions
        {
            Size = GetImageSize(size),
            Quality = GetImageQuality(quality),
            Style = GetImageStyle(style),
            ResponseFormat = GetImageFormat(format)
        };
        return (prompt, count, options);
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    private GeneratedImageSize GetImageSize(string size)
    {
        var value = !string.IsNullOrEmpty(size) ? size : "1024x1024";

        GeneratedImageSize retSize;
        switch (value)
        {
            case "256x256":
                retSize = GeneratedImageSize.W256xH256;
                break;
            case "512x512":
                retSize = GeneratedImageSize.W512xH512;
                break;
            case "1024x1024":
                retSize = GeneratedImageSize.W1024xH1024;
                break;
            case "1024x1792":
                retSize = GeneratedImageSize.W1024xH1792;
                break;
            case "1792x1024":
                retSize = GeneratedImageSize.W1792xH1024;
                break;
            default:
                retSize = GeneratedImageSize.W1024xH1024;
                break;
        }

        return retSize;
    }

    private GeneratedImageQuality GetImageQuality(string quality)
    {
        var value = !string.IsNullOrEmpty(quality) ? quality : "standard";

        GeneratedImageQuality retQuality;
        switch (value)
        {
            case "standard":
                retQuality = GeneratedImageQuality.Standard;
                break;
            case "hd":
                retQuality = GeneratedImageQuality.High;
                break;
            default:
                retQuality = GeneratedImageQuality.Standard;
                break;
        }

        return retQuality;
    }

    private GeneratedImageStyle GetImageStyle(string style)
    {
        var value = !string.IsNullOrEmpty(style) ? style : "natural";

        GeneratedImageStyle retStyle;
        switch (value)
        {
            case "natural":
                retStyle = GeneratedImageStyle.Natural;
                break;
            case "vivid":
                retStyle = GeneratedImageStyle.Vivid;
                break;
            default:
                retStyle = GeneratedImageStyle.Natural;
                break;
        }

        return retStyle;
    }

    private GeneratedImageFormat GetImageFormat(string format)
    {
        var value = !string.IsNullOrEmpty(format) ? format : "uri";

        GeneratedImageFormat retFormat;
        switch (value)
        {
            case "uri":
                retFormat = GeneratedImageFormat.Uri;
                break;
            case "bytes":
                retFormat = GeneratedImageFormat.Bytes;
                break;
            default:
                retFormat = GeneratedImageFormat.Uri;
                break;
        }

        return retFormat;
    }

    private int GetImageCount(string count)
    {
        if (!int.TryParse(count, out var retCount))
        {
            return DEFAULT_IMAGE_COUNT;
        }

        return retCount > 0 && retCount <= IMAGE_COUNT_LIMIT ? retCount : DEFAULT_IMAGE_COUNT;
    }
}