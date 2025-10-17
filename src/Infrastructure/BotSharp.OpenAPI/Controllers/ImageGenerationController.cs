using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Instructs.Options;
using BotSharp.OpenAPI.ViewModels.Instructs;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class ImageGenerationController
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InstructModeController> _logger;

    public ImageGenerationController(IServiceProvider services, ILogger<InstructModeController> logger)
    {
        _services = services;
        _logger = logger;
    }

    [HttpPost("/instruct/image-composition")]
    public async Task<ImageGenerationViewModel> ComposeImages([FromBody] ImageCompositionRequest request)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        request.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            if (request.Files.Length < 1)
            {
                return new ImageGenerationViewModel { Message = "No image found" };
            }

            var message = await fileInstruct.ComposeImages(request.Text, request.Files, new InstructOptions
            {
                Provider = request.Provider,
                Model = request.Model,
                AgentId = request.AgentId,
                TemplateName = request.TemplateName,
                ImageConvertProvider = request.ImageConvertProvider
            });
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages?.Select(x => ImageViewModel.ToViewModel(x)) ?? [];
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image edit. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }
}
