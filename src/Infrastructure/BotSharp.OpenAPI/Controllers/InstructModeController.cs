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
            .SetState("input_text", input.Text, source: StateSource.External)
            .SetState("template_name", input.Template, source: StateSource.External);

        var instructor = _services.GetRequiredService<IInstructService>();
        var result = await instructor.Execute(agentId,
            new RoleDialogModel(AgentRole.User, input.Text),
            templateName: input.Template,
            instruction: input.Instruction,
            files: input.Files);

        result.States = state.GetStates();

        return result; 
    }

    [HttpPost("/instruct/text-completion")]
    public async Task<string> TextCompletion([FromBody] IncomingInstructRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        state.SetState("provider", input.Provider ?? "azure-openai", source: StateSource.External)
            .SetState("model", input.Model, source: StateSource.External)
            .SetState("model_id", input.ModelId, source: StateSource.External);

        var agentId = input.AgentId ?? Guid.Empty.ToString();
        var textCompletion = CompletionProvider.GetTextCompletion(_services);
        var response = await textCompletion.GetCompletion(input.Text, agentId, Guid.NewGuid().ToString());

        await HookEmitter.Emit<IInstructHook>(_services, async hook =>
            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = agentId,
                Provider = textCompletion.Provider,
                Model = textCompletion.Model,
                TemplateName = input.Template,
                UserMessage = input.Text,
                CompletionText = response
            }), agentId);

        return response;
    }

    #region Chat
    [HttpPost("/instruct/chat-completion")]
    public async Task<string> ChatCompletion([FromBody] IncomingInstructRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        state.SetState("provider", input.Provider, source: StateSource.External)
            .SetState("model", input.Model, source: StateSource.External)
            .SetState("model_id", input.ModelId, source: StateSource.External);

        var agentId = input.AgentId ?? Guid.Empty.ToString();
        var completion = CompletionProvider.GetChatCompletion(_services);
        var message = await completion.GetChatCompletions(new Agent()
        {
            Id = agentId,
            Instruction = input.Instruction
        }, new List<RoleDialogModel>
        {
            new RoleDialogModel(AgentRole.User, input.Text)
            {
                Files = input.Files?.Select(x => new BotSharpFile
                {
                    FileUrl = x.FileUrl,
                    FileData = x.FileData,
                    ContentType = x.ContentType
                }).ToList() ?? []
            }
        });

        await HookEmitter.Emit<IInstructHook>(_services, async hook =>
            await hook.OnResponseGenerated(new InstructResponseModel
            {
                AgentId = agentId,
                Provider = completion.Provider,
                Model = completion.Model,
                TemplateName = input.Template,
                UserMessage = input.Text,
                SystemInstruction = message.RenderedInstruction,
                CompletionText = message.Content
            }), agentId);

        return message.Content;
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

    [HttpPost("/instruct/multi-modal/upload")]
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
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in reading multi-modal files. {ex.Message}";
            _logger.LogError(ex, error);
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
        input.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var message = await fileInstruct.GenerateImage(input.Text, new InstructOptions
            {
                Provider = input.Provider,
                Model = input.Model,
                AgentId = input.AgentId,
                TemplateName = input.TemplateName
            });
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image generation. {ex.Message}";
            _logger.LogError(ex, error);
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
        input.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            if (input.File == null)
            {
                return new ImageGenerationViewModel { Message = "Error! Cannot find an image!" };
            }

            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var message = await fileInstruct.VaryImage(input.File, new InstructOptions
            {
                Provider = input.Provider,
                Model = input.Model,
                AgentId = input.AgentId
            });
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();

            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image variation. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-variation/upload")]
    public async Task<ImageGenerationViewModel> ImageVariation(IFormFile file, [FromForm] MultiModalRequest request)
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
                AgentId = request?.AgentId
            });

            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image variation upload. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-edit")]
    public async Task<ImageGenerationViewModel> ImageEdit([FromBody] ImageEditRequest input)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            if (input.File == null)
            {
                return new ImageGenerationViewModel { Message = "Error! Cannot find a valid image file!" };
            }
            var message = await fileInstruct.EditImage(input.Text, input.File, new InstructOptions
            {
                Provider = input.Provider,
                Model = input.Model,
                AgentId = input.AgentId,
                TemplateName = input.TemplateName
            });
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
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

    [HttpPost("/instruct/image-edit/upload")]
    public async Task<ImageGenerationViewModel> ImageEdit(IFormFile file, [FromForm] MultiModalRequest request)
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
                TemplateName = request?.TemplateName
            });

            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();

            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image edit upload. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-mask-edit")]
    public async Task<ImageGenerationViewModel> ImageMaskEdit([FromBody] ImageMaskEditRequest input)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            var image = input.File;
            var mask = input.Mask;
            if (image == null || mask == null)
            {
                return new ImageGenerationViewModel { Message = "Error! Cannot find a valid image or mask!" };
            }
            var message = await fileInstruct.EditImage(input.Text, image, mask, new InstructOptions
            {
                Provider = input.Provider,
                Model = input.Model,
                AgentId = input.AgentId,
                TemplateName = input.TemplateName
            });
            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();
            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image mask edit. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }

    [HttpPost("/instruct/image-mask-edit/upload")]
    public async Task<ImageGenerationViewModel> ImageMaskEdit(IFormFile image, IFormFile mask, [FromForm] MultiModalRequest request)
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
                    TemplateName = request?.TemplateName
                });

            imageViewModel.Content = message.Content;
            imageViewModel.Images = message.GeneratedImages.Select(x => ImageViewModel.ToViewModel(x)).ToList();

            return imageViewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in image mask edit upload. {ex.Message}";
            _logger.LogError(ex, error);
            imageViewModel.Message = error;
            return imageViewModel;
        }
    }
    #endregion

    #region Pdf
    [HttpPost("/instruct/pdf-completion")]
    public async Task<PdfCompletionViewModel> PdfCompletion([FromBody] MultiModalFileRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var viewModel = new PdfCompletionViewModel();

        try
        {
            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var content = await fileInstruct.ReadPdf(input.Text, input.Files, new InstructOptions
            {
                Provider = input.Provider,
                Model = input.Model,
                AgentId = input.AgentId,
                TemplateName = input.TemplateName
            });
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in pdf completion. {ex.Message}";
            _logger.LogError(ex, error);
            viewModel.Message = error;
            return viewModel;
        }
    }

    [HttpPost("/instruct/pdf-completion/upload")]
    public async Task<PdfCompletionViewModel> PdfCompletion([FromForm] IEnumerable<IFormFile> files, [FromForm] MultiModalRequest request)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        request?.States?.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var viewModel = new PdfCompletionViewModel();

        try
        {
            var fileModels = files.Select(x => new InstructFileModel
            {
                FileData = FileUtility.BuildFileDataFromFile(x),
                ContentType = x.ContentType
            }).ToList();

            var fileInstruct = _services.GetRequiredService<IFileInstructService>();
            var content = await fileInstruct.ReadPdf(request?.Text ?? string.Empty, fileModels, new InstructOptions
            {
                Provider = request?.Provider,
                Model = request?.Model,
                AgentId = request?.AgentId,
                TemplateName = request?.TemplateName
            });
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in pdf completion. {ex.Message}";
            _logger.LogError(ex, error);
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
        input.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var viewModel = new SpeechToTextViewModel();

        try
        {
            var audio = input.File;
            if (audio == null)
            {
                return new SpeechToTextViewModel { Message = "Error! Cannot find a valid audio file!" };
            }
            var content = await fileInstruct.SpeechToText(audio, input.Text, new InstructOptions
            {
                Provider = input.Provider,
                Model = input.Model,
                AgentId = input.AgentId,
                TemplateName = input.TemplateName
            });
            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in speech to text. {ex.Message}";
            _logger.LogError(ex, error);
            viewModel.Message = error;
            return viewModel;
        }
    }

    [HttpPost("/instruct/speech-to-text/upload")]
    public async Task<SpeechToTextViewModel> SpeechToText(IFormFile file, [FromForm] MultiModalRequest request)
    {
        var fileInstruct = _services.GetRequiredService<IFileInstructService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        request?.States?.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));
        var viewModel = new SpeechToTextViewModel();

        try
        {
            var audioData = FileUtility.BuildFileDataFromFile(file);
            var content = await fileInstruct.SpeechToText(new InstructFileModel
            { 
                FileData = audioData,
                FileName = Path.GetFileNameWithoutExtension(file.FileName),
                FileExtension = Path.GetExtension(file.FileName)
            },
            request?.Text ?? string.Empty,
            new InstructOptions
            {
                Provider = request?.Provider,
                Model = request?.Model,
                AgentId = request?.AgentId,
                TemplateName = request?.TemplateName
            });

            viewModel.Content = content;
            return viewModel;
        }
        catch (Exception ex)
        {
            var error = $"Error in speech-to-text. {ex.Message}";
            _logger.LogError(ex, error);
            viewModel.Message = error;
            return viewModel;
        }
    }

    [HttpPost("/instruct/text-to-speech")]
    public async Task<IActionResult> TextToSpeech([FromBody] TextToSpeechRequest input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, source: StateSource.External));

        var completion = CompletionProvider.GetAudioSynthesizer(_services, provider: input.Provider, model: input.Model);
        var binaryData = await completion.GenerateAudioAsync(input.Text);
        var stream = binaryData.ToStream();
        stream.Position = 0;

        return new FileStreamResult(stream, "audio/mpeg") { FileDownloadName = "output.mp3" };
    }
    #endregion
}
