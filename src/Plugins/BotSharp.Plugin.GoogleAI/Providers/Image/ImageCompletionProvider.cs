using System.IO;

namespace BotSharp.Plugin.GoogleAI.Providers.Image;

public partial class ImageCompletionProvider : IImageCompletion
{
    protected readonly GoogleAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger<ImageCompletionProvider> _logger;

    private const int DEFAULT_IMAGE_COUNT = 1;
    private const int IMAGE_COUNT_LIMIT = 5;

    protected string _model;

    public virtual string Provider => "google-ai";
    public string Model => _model;

    public ImageCompletionProvider(
        GoogleAiSettings settings,
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

    public Task<RoleDialogModel> GetImageGeneration(Agent agent, RoleDialogModel message)
    {
        throw new NotImplementedException();
    }

    public Task<RoleDialogModel> GetImageVariation(Agent agent, RoleDialogModel message, Stream image, string imageFileName)
    {
        throw new NotImplementedException();
    }

    public Task<RoleDialogModel> GetImageEdits(Agent agent, RoleDialogModel message, Stream image, string imageFileName)
    {
        throw new NotImplementedException();
    }

    public Task<RoleDialogModel> GetImageEdits(Agent agent, RoleDialogModel message, Stream image, string imageFileName, Stream mask, string maskFileName)
    {
        throw new NotImplementedException();
    }

    public Task<RoleDialogModel> GetImageComposition(Agent agent, RoleDialogModel message, Stream[] images, string[] imageFileNames)
    {
        throw new NotImplementedException();
    }
}
