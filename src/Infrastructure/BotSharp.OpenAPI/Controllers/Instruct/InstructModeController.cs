using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Core.Infrastructures;
using BotSharp.OpenAPI.ViewModels.Instructs;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public partial class InstructModeController : ControllerBase
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
            .SetState("template_name", input.Template, source: StateSource.External)
            .SetState("channel", input.Channel, source: StateSource.External)
            .SetState("code_options", input.CodeOptions, source: StateSource.External)
            .SetState("file_options", input.FileOptions, source: StateSource.External)
            .SetState("file_count", !input.Files.IsNullOrEmpty() ? input.Files.Count : (int?)null, source: StateSource.External);

        var instructor = _services.GetRequiredService<IInstructService>();
        var result = await instructor.Execute(agentId,
            new RoleDialogModel(AgentRole.User, input.Text),
            instruction: input.Instruction,
            templateName: input.Template,
            files: input.Files,
            codeOptions: input.CodeOptions,
            fileOptions: input.FileOptions);

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
}
