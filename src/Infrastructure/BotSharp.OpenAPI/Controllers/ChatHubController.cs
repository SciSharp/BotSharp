using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.OpenAPI.ViewModels.Conversations;
using Microsoft.AspNetCore.SignalR;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class ChatHubController : ControllerBase, IApiAdapter
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly IHubContext<ChatHub> _chatHub;
    private readonly IUserIdentity _user;

    public ChatHubController(IServiceProvider services,
        ILogger<ChatHubController> logger,
        IHubContext<ChatHub> chatHub,
        IUserIdentity user)
    {
        _services = services;
        _logger = logger;
        _chatHub = chatHub;
        _user = user;
    }

    [HttpPost("/chat-hub/client/{agentId}/{conversationId}")]
    public async Task OnMessageReceivedFromClient([FromRoute] string agentId, 
        [FromRoute] string conversationId, 
        [FromBody] NewMessageModel input)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(conversationId, input.States);
        conv.States.SetState("channel", input.Channel);

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

        var state = _services.GetRequiredService<IConversationStateService>();
        response.States = state.GetStates();
        response.MessageId = inputMsg.MessageId;

        // Update console conversation UI for CSR
        await _chatHub.Clients.All.SendAsync("OnMessageReceivedFromClient", new RoleDialogModel(AgentRole.User, input.Text)
        {
            
        });

        await _chatHub.Clients.All.SendAsync("OnMessageReceivedFromAssistant", new RoleDialogModel(AgentRole.Assistant, response.Text)
        {

        });
    }

    [HttpPost("/chat-hub/csr/{agentId}/{conversationId}")]
    public async Task OnMessageReceivedFromCsr([FromRoute] string agentId,
        [FromRoute] string conversationId, 
        [FromBody] NewMessageModel input)
    {
        // Update console conversation UI for User
        await _chatHub.Clients.All.SendAsync("OnMessageReceivedFromCsr", new RoleDialogModel(AgentRole.CSR, input.Text)
        {

        });
    }
}
