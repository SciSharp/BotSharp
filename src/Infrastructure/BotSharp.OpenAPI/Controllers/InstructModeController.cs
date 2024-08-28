using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Core.Infrastructures;
using BotSharp.OpenAPI.ViewModels.Instructs;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class InstructModeController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly ILogger<InstructModeController> _logger;

    public InstructModeController(IServiceProvider services, ILogger<InstructModeController> logger)
    {
        _services = services;
        _logger = logger;
    }

    [HttpPost("/instruct/{agentId}")]
    public async Task<InstructResult> InstructCompletion([FromRoute] string agentId, [FromBody] InstructMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        state.SetState("provider", input.Provider, source: StateSource.External)
            .SetState("model", input.Model, source: StateSource.External)
            .SetState("model_id", input.ModelId, source: StateSource.External)
            .SetState("instruction", input.Instruction, source: StateSource.External)
            .SetState("input_text", input.Text,source: StateSource.External);

        var instructor = _services.GetRequiredService<IInstructService>();
        var result = await instructor.Execute(agentId,
            new RoleDialogModel(AgentRole.User, input.Text),
            templateName: input.Template,
            instruction: input.Instruction);

        result.States = state.GetStates();

        return result; 
    }

    [HttpPost("/instruct/text-completion")]
    public async Task<string> TextCompletion([FromBody] IncomingMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        state.SetState("provider", input.Provider ?? "azure-openai", source: StateSource.External)
            .SetState("model", input.Model, source: StateSource.External)
            .SetState("model_id", input.ModelId, source: StateSource.External);

        var textCompletion = CompletionProvider.GetTextCompletion(_services);
        return await textCompletion.GetCompletion(input.Text, Guid.Empty.ToString(), Guid.NewGuid().ToString());
    }

    #region Chat
    [HttpPost("/instruct/chat-completion")]
    public async Task<string> ChatCompletion([FromBody] IncomingMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        state.SetState("provider", input.Provider, source: StateSource.External)
            .SetState("model", input.Model, source: StateSource.External)
            .SetState("model_id", input.ModelId, source: StateSource.External);

        var textCompletion = CompletionProvider.GetChatCompletion(_services);
        var message = await textCompletion.GetChatCompletions(new Agent()
        {
            Id = Guid.Empty.ToString(),
        }, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, input.Text)
        });
        return message.Content;
    }
    #endregion

    #region Read image
    [HttpPost("/instruct/multi-modal")]
    public async Task<string> MultiModalCompletion([FromBody] MultiModalRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));

        try
        {
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var content = await fileInstruct.ReadImages(input.Provider, input.Model, input.Text, input.Files);
            return content;
        }
        catch (Exception ex)
        {
            var error = $"Error in reading images. {ex.Message}";
            _logger.LogError(error);
            return error;
        }
    }

    [HttpPost("/instruct/multi-modal/upload")]
    public async Task<MultiModalViewModel> MultiModalCompletion(IFormFile file, [FromForm] string text, [FromForm] string? provider = null,
        [FromForm] string? model = null, [FromForm] List<MessageState>? states = null)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        states?.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var viewModel = new MultiModalViewModel();

        try
        {
            var data = FileUtility.BuildFileDataFromFile(file);
            var files = new List<InstructFileModel>
            {
                new InstructFileModel { FileData = data }
            };
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var content = await fileInstruct.ReadImages(provider, model, text, files);
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in reading image upload. {ex.Message}";
            _logger.LogError(error);
            viewModel.Message = error;
            return viewModel;
        }
    }
    #endregion

    #region Generate image
    [HttpPost("/instruct/image-generation")]
    public async Task<ImageGenerationViewModel> ImageGeneration([FromBody] ImageGenerationRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var message = await fileInstruct.GenerateImage(input.Provider, input.Model, input.Text);
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image generation. {ex.Message}";
            _logger.LogError(error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }
    #endregion

    #region Edit image
    [HttpPost("/instruct/image-variation")]
    public async Task<ImageGenerationViewModel> ImageVariation([FromBody] ImageVariationRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            if (input.File == null)
            {
                return new ImageGenerationViewModel { Message = "Error! Cannot find an image!" };
            }

            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var message = await fileInstruct.VaryImage(input.Provider, input.Model, input.File);
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();

            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image variation. {ex.Message}";
            _logger.LogError(error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-variation/upload")]
    public async Task<ImageGenerationViewModel> ImageVariation(IFormFile file, [FromForm] string? provider = null,
        [FromForm] string? model = null, [FromForm] List<MessageState>? states = null)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        states?.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            using var stream = new MemoryStream();
            file.CopyTo(stream);
            stream.Position = 0;

            var completion = CompletionProvider.GetImageCompletion(_services, provider: provider ?? "openai", model: model ?? "dall-e-2");
            var message = await completion.GetImageVariation(new Agent()
            {
                Id = Guid.Empty.ToString()
            }, new RoleDialogModel(AgentRole.User, string.Empty), stream, file.FileName);

            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            stream.Close();

            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image variation upload. {ex.Message}";
            _logger.LogError(error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-edit")]
    public async Task<ImageGenerationViewModel> ImageEdit([FromBody] ImageEditRequest input)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            if (input.File == null)
            {
                return new ImageGenerationViewModel { Message = "Error! Cannot find a valid image file!" };
            }
            var message = await fileInstruct.EditImage(input.Provider, input.Model, input.Text, input.File);
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image edit. {ex.Message}";
            _logger.LogError(error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-edit/upload")]
    public async Task<ImageGenerationViewModel> ImageEdit(IFormFile file, [FromForm] string text, [FromForm] string? provider = null,
        [FromForm] string? model = null, [FromForm] List<MessageState>? states = null)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        states?.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            using var stream = new MemoryStream();
            file.CopyTo(stream);
            stream.Position = 0;

            var completion = CompletionProvider.GetImageCompletion(_services, provider: provider ?? "openai", model: model ?? "dall-e-2");
            var message = await completion.GetImageEdits(new Agent()
            {
                Id = Guid.Empty.ToString()
            }, new RoleDialogModel(AgentRole.User, text), stream, file.FileName);

            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            stream.Close();

            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image edit upload. {ex.Message}";
            _logger.LogError(error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-mask-edit")]
    public async Task<ImageGenerationViewModel> ImageMaskEdit([FromBody] ImageMaskEditRequest input)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            var image = input.File;
            var mask = input.Mask;
            if (image == null || mask == null)
            {
                return new ImageGenerationViewModel { Message = "Error! Cannot find a valid image or mask!" };
            }
            var message = await fileInstruct.EditImage(input.Provider, input.Model, input.Text, image, mask);
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image mask edit. {ex.Message}";
            _logger.LogError(error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-mask-edit/upload")]
    public async Task<ImageGenerationViewModel> ImageMaskEdit(IFormFile image, IFormFile mask, [FromForm] string text, [FromForm] string? provider = null,
        [FromForm] string? model = null, [FromForm] List<MessageState>? states = null)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        states?.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            using var imageStream = new MemoryStream();
            image.CopyTo(imageStream);
            imageStream.Position = 0;

            using var maskStream = new MemoryStream();
            mask.CopyTo(maskStream);
            maskStream.Position = 0;

            var completion = CompletionProvider.GetImageCompletion(_services, provider: provider ?? "openai", model: model ?? "dall-e-2");
            var message = await completion.GetImageEdits(new Agent()
            {
                Id = Guid.Empty.ToString()
            }, new RoleDialogModel(AgentRole.User, text), imageStream, image.FileName, maskStream, mask.FileName);

            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            imageStream.Close();
            maskStream.Close();

            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image mask edit upload. {ex.Message}";
            _logger.LogError(error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }
    #endregion

    #region Pdf
    [HttpPost("/instruct/pdf-completion")]
    public async Task<PdfCompletionViewModel> PdfCompletion([FromBody] MultiModalRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var viewModel = new PdfCompletionViewModel();

        try
        {
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var content = await fileInstruct.ReadPdf(input.Provider, input.Model, input.ModelId, input.Text, input.Files);
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in pdf completion. {ex.Message}";
            _logger.LogError(error);
            viewModel.Message = error;
            return viewModel;
        }
    }

    [HttpPost("/instruct/pdf-completion/upload")]
    public async Task<PdfCompletionViewModel> PdfCompletion(IFormFile file, [FromForm] string text, [FromForm] string? provider = null,
        [FromForm] string? model = null, [FromForm] string? modelId = null, [FromForm] List<MessageState>? states = null)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        states?.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var viewModel = new PdfCompletionViewModel();

        try
        {
            var data = FileUtility.BuildFileDataFromFile(file);
            var files = new List<InstructFileModel>
            {
                new InstructFileModel { FileData = data }
            };

            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var content = await fileInstruct.ReadPdf(provider, model, modelId, text, files);
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in pdf completion upload. {ex.Message}";
            _logger.LogError(error);
            viewModel.Message = error;
            return viewModel;
        }
    }
    #endregion

    #region Audio
    [HttpPost("/instruct/speech-to-text")]
    public async Task<SpeechToTextViewModel> SpeechToText([FromBody] SpeechToTextRequest input)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var viewModel = new SpeechToTextViewModel();

        try
        {
            var audio = input.File;
            if (audio == null)
            {
                return new SpeechToTextViewModel { Message = "Error! Cannot find a valid audio file!" };
            }
            var content = await fileInstruct.SpeechToText(input.Provider, input.Model, audio);
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in speech to text. {ex.Message}";
            _logger.LogError(error);
            viewModel.Message = error;
            return viewModel;
        }
    }

    [HttpPost("/instruct/speech-to-text/upload")]
    public async Task<SpeechToTextViewModel> SpeechToText(IFormFile file, [FromForm] string? provider = null, [FromForm] string? model = null,
       [FromForm] string? text = null, [FromForm] List<MessageState>? states = null)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        states?.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var viewModel = new SpeechToTextViewModel();

        try
        {
            using var stream = new MemoryStream();
            file.CopyTo(stream);
            stream.Position = 0;

            var completion = CompletionProvider.GetAudioCompletion(_services, provider: provider ?? "openai", model: model ?? "whisper-1");
            var content = await completion.GenerateTextFromAudioAsync(stream, file.FileName, text);
            viewModel.Content = content;
            stream.Close();
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in speech-to-text upload. {ex.Message}";
            _logger.LogError(error);
            viewModel.Message = error;
            return viewModel;
        }
    }
    #endregion
}
