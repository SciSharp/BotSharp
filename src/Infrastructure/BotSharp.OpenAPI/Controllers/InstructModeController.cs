using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
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
        var instructor = _services.GetRequiredService<IInstructService>();
        var agentService = _services.GetRequiredService<IAgentService>();
        Agent agent = await agentService.LoadAgent(agentId);

        // switch to different instruction template
        if (!string.IsNullOrEmpty(input.Template))
        {
            var agentSettings = _services.GetRequiredService<AgentSettings>();
            var filePath = Path.Combine(agentService.GetAgentDataDir(agentId), $"{input.Template}.{agentSettings.TemplateFormat}");
            agent.Instruction = System.IO.File.ReadAllText(filePath);
        }

        var conv = _services.GetRequiredService<IConversationService>();
        conv.States.SetState("provider", input.Provider)
            .SetState("model", input.Model);

        return await instructor.ExecuteInstruction(agent,
            new RoleDialogModel(AgentRole.User, input.Text),
            fn => Task.CompletedTask,
            fn => Task.CompletedTask,
            fn => Task.CompletedTask);
    }
}
