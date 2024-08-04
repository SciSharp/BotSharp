using OpenAI.Images;

namespace BotSharp.Plugin.AzureOpenAI.Providers;

public class ImageGenerationProvider : IImageGeneration
{
    protected readonly AzureOpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;

    protected string _model;

    public virtual string Provider => "azure-openai";

    public ImageGenerationProvider(
        AzureOpenAiSettings settings,
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
        var (prompt, options) = PrepareOptions(conversations);
        var imageClient = client.GetImageClient(_model);

        var response = imageClient.GenerateImage(prompt, options);
        var value = response.Value;

        var content = string.Empty;
        if (!string.IsNullOrEmpty(value.RevisedPrompt))
        {
            content = value.RevisedPrompt;
        }

        var responseMessage = new RoleDialogModel(AgentRole.Assistant, content)
        {
            CurrentAgentId = agent.Id,
            MessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty,
            Data = options.ResponseFormat == GeneratedImageFormat.Uri ? value.ImageUri?.AbsoluteUri : value.ImageBytes
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

    private (string, ImageGenerationOptions) PrepareOptions(List<RoleDialogModel> conversations)
    {
        var prompt = conversations.LastOrDefault()?.Payload ?? conversations.LastOrDefault()?.Content ?? string.Empty;

        var state = _services.GetRequiredService<IConversationStateService>();
        var size = state.GetState("image_size");
        var quality = state.GetState("image_quality");
        var style = state.GetState("image_style");

        var options = new ImageGenerationOptions
        {
            Size = GetImageSize(size),
            Quality = GetImageQuality(quality),
            Style = GetImageStyle(style),
            ResponseFormat = GeneratedImageFormat.Uri
        };
        return (prompt, options);
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
            case "standard":
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
}
