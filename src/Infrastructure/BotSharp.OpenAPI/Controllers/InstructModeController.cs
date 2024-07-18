using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Core.Infrastructures;
using BotSharp.OpenAPI.ViewModels.Instructs;
using NetTopologySuite.IO;

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
    public async Task<InstructResult> InstructCompletion([FromRoute] string agentId,
        [FromBody] InstructMessageModel input)
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
        state.SetState("provider", input.Provider, source: StateSource.External)
            .SetState("model", input.Model, source: StateSource.External)
            .SetState("model_id", input.ModelId, source: StateSource.External);

        var textCompletion = CompletionProvider.GetTextCompletion(_services);
        return await textCompletion.GetCompletion(input.Text, Guid.Empty.ToString(), Guid.NewGuid().ToString());
    }

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

    [HttpPost("/instruct/multi-modal")]
    public async Task<string> MultiModalCompletion([FromBody] IncomingMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));

        try
        {
            var completion = CompletionProvider.GetChatCompletion(_services, provider: input.Provider ?? "openai",
                model: input.Model ?? "gpt-4o", multiModal: true);
            var message = await completion.GetChatCompletions(new Agent()
            {
                Id = Guid.Empty.ToString(),
            }, new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, input.Text)
                {
                    Files = input.Files
                }
            });
            return message.Content;
        }
        catch (Exception ex)
        {
            var error = $"Error in analyzing files. {ex.Message}";
            _logger.LogError(error);
            return error;
        }
    }

    [HttpPost("/instruct/image-generation")]
    public async Task<ImageGenerationViewModel> ImageGeneration([FromBody] IncomingMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var imageViewModel = new ImageGenerationViewModel();

        try
        {
            var completion = CompletionProvider.GetImageGeneration(_services, provider: input.Provider ?? "openai", model: input.Model ?? "dall-e-3");
            var message = await completion.GetImageGeneration(new Agent()
            {
                Id = Guid.Empty.ToString(),
            }, new List<RoleDialogModel>
            {
                new RoleDialogModel(AgentRole.User, input.Text)
            });
            
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

    [HttpPost("/instruct/pdf-completion")]
    public async Task<PdfCompletionViewModel> PdfCompletion([FromBody] IncomingMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Key, x.Value, activeRounds: x.ActiveRounds, source: StateSource.External));
        var viewModel = new PdfCompletionViewModel();

        try
        {
            var fileService = _services.GetRequiredService<IBotSharpFileService>();
            var content = await fileService.InstructPdf(input.Provider, input.Model, input.ModelId, input.Text, input.Files);
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
}
