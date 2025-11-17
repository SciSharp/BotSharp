using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Instructs.Options;
using BotSharp.OpenAPI.ViewModels.Instructs;

namespace BotSharp.OpenAPI.Controllers;


public partial class InstructModeController
{
    #region Image composition
    [HttpPost("/instruct/image-composition")]
    public async Task<ImageGenerationViewModel> ComposeImages([FromBody] ImageCompositionRequest request)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        request.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            if (request.Files.IsNullOrEmpty())
            {
                return new ImageGenerationViewModel { ErrorMsg = "No image found" };
            }

            var message = await fileInstruct.ComposeImages(request.Text, request.Files, new InstructOptions
            {
                Provider = request.Provider,
                Model = request.Model,
                AgentId = request.AgentId,
                TemplateName = request.TemplateName,
                ImageConverter = request.ImageConverter
            });

            imageViewModel.Success = true;
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages?.Select(x => ImageViewModel.ToViewModel(x)) ?? [];
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image composition. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.ErrorMsg = error;
            return imageViewModel;
        }
    }
    #endregion

    #region Image generation
    [HttpPost("/instruct/image-generation")]
    public async Task<ImageGenerationViewModel> ImageGeneration([FromBody] ImageGenerationRequest request)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        request.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var message = await fileInstruct.GenerateImage(request.Text, new InstructOptions
            {
                Provider = request.Provider,
                Model = request.Model,
                AgentId = request.AgentId,
                TemplateName = request.TemplateName
            });

            imageViewModel.Success = true;
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages?.Select(x => ImageViewModel.ToViewModel(x)) ?? [];
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image generation. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.ErrorMsg = error;
            return imageViewModel;
        }
    }
    #endregion

    #region Image variation
    [HttpPost("/instruct/image-variation")]
    public async Task<ImageGenerationViewModel> ImageVariation([FromBody] ImageVariationFileRequest request)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        request.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            if (request.File == null)
            {
                return new ImageGenerationViewModel { ErrorMsg = "Error! Cannot find an image!" };
            }

            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var message = await fileInstruct.VaryImage(request.File, new InstructOptions
            {
                Provider = request.Provider,
                Model = request.Model,
                AgentId = request.AgentId,
                ImageConverter = request.ImageConverter
            });

            imageViewModel.Success = true;
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages?.Select(x => ImageViewModel.ToViewModel(x)) ?? [];
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image variation. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.ErrorMsg = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-variation/form")]
    public async Task<ImageGenerationViewModel> ImageVariation(IFormFile file, [FromForm] ImageVariationRequest request)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        request?.States?.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var fileData = FileUtility.BuildFileDataFromFile(file);
            var message = await fileInstruct.VaryImage(new InstructFileModel
            {
                FileData = fileData,
                FileName = Path.GetFileNameWithoutExtension(file.FileName),
                FileExtension = Path.GetExtension(file.FileName)
            },
            new InstructOptions
            {
                Provider = request?.Provider,
                Model = request?.Model,
                AgentId = request?.AgentId,
                ImageConverter = request?.ImageConverter
            });

            imageViewModel.Success = true;
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages?.Select(x => ImageViewModel.ToViewModel(x)) ?? [];
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image variation upload. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.ErrorMsg = error;
            return imageViewModel;
        }
    }
    #endregion

    #region Image edit
    [HttpPost("/instruct/image-edit")]
    public async Task<ImageGenerationViewModel> ImageEdit([FromBody] ImageEditFileRequest request)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        request.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            if (request.File == null)
            {
                return new ImageGenerationViewModel { ErrorMsg = "Error! Cannot find a valid image file!" };
            }

            var message = await fileInstruct.EditImage(request.Text, request.File, new InstructOptions
            {
                Provider = request.Provider,
                Model = request.Model,
                AgentId = request.AgentId,
                TemplateName = request.TemplateName,
                ImageConverter = request.ImageConverter
            });

            imageViewModel.Success = true;
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages?.Select(x => ImageViewModel.ToViewModel(x)) ?? [];
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image edit. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.ErrorMsg = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-edit/form")]
    public async Task<ImageGenerationViewModel> ImageEdit(IFormFile file, [FromForm] ImageEditRequest request)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        request?.States?.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            var fileData = FileUtility.BuildFileDataFromFile(file);
            var message = await fileInstruct.EditImage(request?.Text ?? string.Empty, new InstructFileModel
            {
                FileData = fileData,
                FileName = Path.GetFileNameWithoutExtension(file.FileName),
                FileExtension = Path.GetExtension(file.FileName)
            },
            new InstructOptions
            {
                Provider = request?.Provider,
                Model = request?.Model,
                AgentId = request?.AgentId,
                TemplateName = request?.TemplateName,
                ImageConverter = request?.ImageConverter
            });

            imageViewModel.Success = true;
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages?.Select(x => ImageViewModel.ToViewModel(x)) ?? [];
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image edit upload. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.ErrorMsg = error;
            return imageViewModel;
        }
    }
    #endregion

    #region Image mask edit
    [HttpPost("/instruct/image-mask-edit")]
    public async Task<ImageGenerationViewModel> ImageMaskEdit([FromBody] ImageMaskEditFileRequest request)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        request.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            var image = request.File;
            var mask = request.Mask;
            if (image == null || mask == null)
            {
                return new ImageGenerationViewModel { ErrorMsg = "Error! Cannot find a valid image or mask!" };
            }

            var message = await fileInstruct.EditImage(request.Text, image, mask, new InstructOptions
            {
                Provider = request.Provider,
                Model = request.Model,
                AgentId = request.AgentId,
                TemplateName = request.TemplateName,
                ImageConverter = request.ImageConverter
            });

            imageViewModel.Success = true;
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages?.Select(x => ImageViewModel.ToViewModel(x)) ?? [];
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image mask edit. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.ErrorMsg = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-mask-edit/form")]
    public async Task<ImageGenerationViewModel> ImageMaskEdit(IFormFile image, IFormFile mask, [FromForm] ImageMaskEditRequest request)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        request?.States?.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            var imageData = FileUtility.BuildFileDataFromFile(image);
            var maskData = FileUtility.BuildFileDataFromFile(mask);
            var message = await fileInstruct.EditImage(request?.Text ?? string.Empty,
                new InstructFileModel
                {
                    FileData = imageData,
                    FileName = Path.GetFileNameWithoutExtension(image.FileName),
                    FileExtension = Path.GetExtension(image.FileName)
                },
                new InstructFileModel
                {
                    FileData = maskData,
                    FileName = Path.GetFileNameWithoutExtension(mask.FileName),
                    FileExtension = Path.GetExtension(mask.FileName)
                },
                new InstructOptions
                {
                    Provider = request?.Provider,
                    Model = request?.Model,
                    AgentId = request?.AgentId,
                    TemplateName = request?.TemplateName,
                    ImageConverter = request?.ImageConverter
                });

            imageViewModel.Success = true;
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages?.Select(x => ImageViewModel.ToViewModel(x)) ?? [];
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image mask edit upload. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.ErrorMsg = error;
            return imageViewModel;
        }
    }
    #endregion

    #region Read image
    [HttpPost("/instruct/multi-modal")]
    public async Task<string> MultiModalCompletion([FromBody] MultiModalFileRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));

        try
        {
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var content = await fileInstruct.ReadImages(input.Text, input.Files, new InstructOptions
            {
                Provider = input.Provider,
                Model = input.Model,
                AgentId = input.AgentId,
                TemplateName = input.TemplateName
            });
            return content;
        }
        catch (Exception ex)
        {
            var error = $"Error in reading multi-modal files. {ex.Message}";
            _logger.LogError(ex, error);
            return error;
        }
    }

    [HttpPost("/instruct/multi-modal/form")]
    public async Task<MultiModalViewModel> MultiModalCompletion([FromForm] IEnumerable<IFormFile> files, [FromForm] MultiModalRequest request)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        request?.States?.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var viewModel = new MultiModalViewModel();

        try
        {
            var fileModels = files.Select(x => new InstructFileModel
            {
                FileData = FileUtility.BuildFileDataFromFile(x),
                ContentType = x.ContentType
            }).ToList();

            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var content = await fileInstruct.ReadImages(request?.Text ?? string.Empty, fileModels, new InstructOptions
            {
                Provider = request?.Provider,
                Model = request?.Model,
                AgentId = request?.AgentId,
                TemplateName = request?.TemplateName
            });
            viewModel.Success = true;
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in reading multi-modal files. {ex.Message}";
            _logger.LogError(ex, error);
            viewModel.ErrorMsg = error;
            return viewModel;
        }
    }
    #endregion
}
