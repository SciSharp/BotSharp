using OpenAI.Images;

namespace BotSharp.Plugin.OpenAI.Providers.Image;

public class ImageVariationProvider : IImageVariation
{
    protected readonly OpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger<ImageVariationProvider> _logger;

    private const int DEFAULT_IMAGE_COUNT = 1;
    private const int IMAGE_COUNT_LIMIT = 5;

    protected string _model;

    public virtual string Provider => "openai";

    public ImageVariationProvider(
        OpenAiSettings settings,
        ILogger<ImageVariationProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _services = services;
        _logger = logger;
    }

    public async Task<RoleDialogModel> GetImageVariation(Agent agent, RoleDialogModel message, Stream image, string imageFileName)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var (imageCount, options) = PrepareOptions();
        var imageClient = client.GetImageClient(_model);

        var response = imageClient.GenerateImageVariations(image, imageFileName, imageCount, options);
        var values = response.Value;

        var generatedImages = new List<ImageGeneration>();
        foreach (var value in values)
        {
            if (value == null) continue;

            var generatedImage = new ImageGeneration { Description = value?.RevisedPrompt ?? string.Empty };
            if (options.ResponseFormat == GeneratedImageFormat.Uri)
            {
                generatedImage.ImageUrl = value?.ImageUri?.AbsoluteUri ?? string.Empty;
            }
            else if (options.ResponseFormat == GeneratedImageFormat.Bytes)
            {
                var base64Str = string.Empty;
                var bytes = value?.ImageBytes?.ToArray();
                if (!bytes.IsNullOrEmpty())
                {
                    base64Str = Convert.ToBase64String(bytes);
                }
                generatedImage.ImageData = base64Str;
            }

            generatedImages.Add(generatedImage);
        }

        var content = string.Join("\r\n", generatedImages.Select(x => x.Description));
        var responseMessage = new RoleDialogModel(AgentRole.Assistant, content)
        {
            CurrentAgentId = agent.Id,
            MessageId = message?.MessageId ?? string.Empty,
            GeneratedImages = generatedImages
        };

        return await Task.FromResult(responseMessage);
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    private (int, ImageVariationOptions) PrepareOptions()
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var size = state.GetState("image_size");
        var quality = state.GetState("image_quality");
        var style = state.GetState("image_style");
        var format = state.GetState("image_format");
        var count = GetImageCount(state.GetState("image_count", "1"));

        var options = new ImageVariationOptions
        {
            Size = GetImageSize(size),
            ResponseFormat = GetImageFormat(format)
        };
        return (count, options);
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
