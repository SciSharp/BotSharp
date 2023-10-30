using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Models;
using BotSharp.OpenAPI.ViewModels.Conversations;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class ConversationController : ControllerBase, IApiAdapter
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;

    public ConversationController(IServiceProvider services,
        IUserIdentity user)
    {
        _services = services;
        _user = user;
    }

    [HttpPost("/conversation/{agentId}")]
    public async Task<ConversationViewModel> NewConversation([FromRoute] string agentId, [FromBody] MessageConfig config)
    {
        var service = _services.GetRequiredService<IConversationService>();
        var conv = new Conversation
        {
            AgentId = agentId
        };
        conv = await service.NewConversation(conv);
        config.States.ForEach(x => conv.States[x.Split('=')[0]] = x.Split('=')[1]);

        return ConversationViewModel.FromSession(conv);
    }

    [HttpDelete("/conversation/{agentId}/{conversationId}")]
    public async Task DeleteConversation([FromRoute] string agentId, [FromRoute] string conversationId)
    {
        var service = _services.GetRequiredService<IConversationService>();
    }

    [HttpPost("/conversation/{agentId}/{conversationId}")]
    public async Task<MessageResponseModel> SendMessage([FromRoute] string agentId,
        [FromRoute] string conversationId,
        [FromBody] NewMessageModel input)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(conversationId, input.States);
        conv.States.SetState("channel", input.Channel)
            .SetState("provider", input.Provider)
            .SetState("model", input.Model)
            .SetState("temperature", input.Temperature)
            .SetState("sampling_factor", input.SamplingFactor);

        var response = new MessageResponseModel();
        var inputMsg = new RoleDialogModel("user", input.Text);
        await conv.SendMessage(agentId, inputMsg,
            async msg =>
            {
                response.Text = msg.Content;
                response.Function = msg.FunctionName;
                response.RichContent = msg.RichContent;
                response.Instruction = msg.Instruction;
                response.Data = msg.Data;
            },
            async fnExecuting =>
            {

            },
            async fnExecuted =>
            {

            });

        response.MessageId = inputMsg.MessageId;

        return response;
    }
}
