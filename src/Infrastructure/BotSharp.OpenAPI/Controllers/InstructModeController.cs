using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Instructs;
using BotSharp.Abstraction.Instructs.Models;
using BotSharp.OpenAPI.ViewModels.Conversations;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class InstructModeController : ControllerBase, IApiAdapter
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;

    public InstructModeController(IServiceProvider services, 
        IUserIdentity user)
    {
        _services = services;
        _user = user;
    }

    [HttpPost("/instruct/{agentId}")]
    public async Task<InstructResult> NewConversation([FromRoute] string agentId,
        [FromBody] NewMessageModel input)
    {
        var response = new InstructResult();
        var instructor = _services.GetRequiredService<IInstructService>();
        var agentService = _services.GetRequiredService<IAgentService>();
        Agent agent = await agentService.LoadAgent(agentId);

        await instructor.ExecuteInstructionRecursively(agent,
            new List<RoleDialogModel>
            {
                new RoleDialogModel("user", input.Text)
            },
            async msg =>
            {
                response.Text = msg.Content;
            },
            async fnExecuting =>
            {

            },
            async fnExecuted =>
            {
                response.Function = fnExecuted.FunctionName;
                response.Data = fnExecuted.ExecutionData;
            });

        return response;
    }
}
