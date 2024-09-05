using OpenAI.Images;

namespace BotSharp.Plugin.OpenAI.Providers.Image;

public partial class ImageCompletionProvider : IImageCompletion
{
    protected readonly OpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger<ImageCompletionProvider> _logger;

    private const int DEFAULT_IMAGE_COUNT = 1;
    private const int IMAGE_COUNT_LIMIT = 5;

    protected string _model;

    public virtual string Provider => "openai";

    public ImageCompletionProvider(
        OpenAiSettings settings,
        ILogger<ImageCompletionProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _services = services;
        _logger = logger;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    #region Private methods
    private List<ImageGeneration> GetImageGenerations(GeneratedImageCollection images, GeneratedImageFormat? format)
    {
        var generatedImages = new List<ImageGeneration>();
        foreach (var image in images)
        {
            if (image == null) continue;

            var generatedImage = new ImageGeneration { Description = image?.RevisedPrompt ?? string.Empty };
            if (format == GeneratedImageFormat.Uri)
            {
                generatedImage.ImageUrl = image?.ImageUri?.AbsoluteUri ?? string.Empty;
            }
            else if (format == GeneratedImageFormat.Bytes)
            {
                var base64Str = string.Empty;
                var bytes = image?.ImageBytes?.ToArray();
                if (!bytes.IsNullOrEmpty())
                {
                    base64Str = Convert.ToBase64String(bytes);
                }
                generatedImage.ImageData = base64Str;
            }

            generatedImages.Add(generatedImage);
        }
        return generatedImages;
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

        if (retCount <= 0)
        {
            retCount = DEFAULT_IMAGE_COUNT;
        }
        else if (retCount > IMAGE_COUNT_LIMIT)
        {
            retCount = IMAGE_COUNT_LIMIT;
        }
        return retCount;
    }
    #endregion
}
