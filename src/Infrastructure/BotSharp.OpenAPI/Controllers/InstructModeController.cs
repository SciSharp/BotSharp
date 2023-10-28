using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.Abstraction.Templating;
using BotSharp.Core.Infrastructures;
using BotSharp.OpenAPI.ViewModels.Instructs;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class InstructModeController : ControllerBase, IApiAdapter
{
    private readonly IServiceProvider _services;

    public InstructModeController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpPost("/instruct/{agentId}")]
    public async Task<InstructResult> InstructCompletion([FromRoute] string agentId,
        [FromBody] InstructMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Split('=')[0], x.Split('=')[1]));
        state.SetState("provider", input.Provider)
            .SetState("model", input.Model)
            .SetState("input_text", input.Text);

        var instructor = _services.GetRequiredService<IInstructService>();
        var result = await instructor.Execute(agentId,
            new RoleDialogModel(AgentRole.User, input.Text),
            templateName: input.Template);

        result.States = state.GetStates();

        return result; 
    }

    [HttpPost("/instruct/text-completion")]
    public async Task<string> TextCompletion([FromBody] IncomingMessageModel input)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        input.States.ForEach(x => state.SetState(x.Split('=')[0], x.Split('=')[1]));
        state.SetState("provider", input.Provider)
            .SetState("model", input.Model);

        var textCompletion = CompletionProvider.GetTextCompletion(_services);
        return await textCompletion.GetCompletion(input.Text);
    }
}
