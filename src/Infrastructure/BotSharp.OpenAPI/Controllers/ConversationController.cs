using BotSharp.Abstraction.Routing;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class ConversationController : ControllerBase
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
            AgentId = agentId,
            Channel = ConversationChannel.OpenAPI,
            UserId = _user.Id,
            TaskId = config.TaskId
        };
        conv = await service.NewConversation(conv);
        service.SetConversationId(conv.Id, config.States);

        return ConversationViewModel.FromSession(conv);
    }

    [HttpPost("/conversations")]
    public async Task<PagedItems<ConversationViewModel>> GetConversations([FromBody] ConversationFilter filter)
    {
        var service = _services.GetRequiredService<IConversationService>();
        var conversations = await service.GetConversations(filter);

        var userService = _services.GetRequiredService<IUserService>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var list = conversations.Items
            .Select(x => ConversationViewModel.FromSession(x))
            .ToList();

        foreach (var item in list)
        {
            var user = await userService.GetUser(item.User.Id);
            item.User = UserViewModel.FromUser(user);

            var agent = await agentService.GetAgent(item.AgentId);
            item.AgentName = agent?.Name;
        }

        return new PagedItems<ConversationViewModel>
        {
            Count = conversations.Count,
            Items = list
        };
    }

    [HttpGet("/conversation/{conversationId}/dialogs")]
    public async Task<IEnumerable<ChatResponseModel>> GetDialogs([FromRoute] string conversationId)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(conversationId, new List<MessageState>());
        var history = conv.GetDialogHistory(fromBreakpoint: false);

        var userService = _services.GetRequiredService<IUserService>();
        var agentService = _services.GetRequiredService<IAgentService>();

        var dialogs = new List<ChatResponseModel>();
        foreach (var message in history)
        {
            if (message.Role == AgentRole.User)
            {
                var user = await userService.GetUser(message.SenderId);

                dialogs.Add(new ChatResponseModel
                {
                    ConversationId = conversationId,
                    MessageId = message.MessageId,
                    CreatedAt = message.CreatedAt,
                    Text = !string.IsNullOrEmpty(message.SecondaryContent) ? message.SecondaryContent : message.Content,
                    Data = message.Data,
                    Sender = UserViewModel.FromUser(user)
                });
            }
            else if (message.Role == AgentRole.Assistant)
            {
                var agent = await agentService.GetAgent(message.CurrentAgentId);
                dialogs.Add(new ChatResponseModel
                {
                    ConversationId = conversationId,
                    MessageId = message.MessageId,
                    CreatedAt = message.CreatedAt,
                    Text = !string.IsNullOrEmpty(message.SecondaryContent) ? message.SecondaryContent : message.Content,
                    Function = message.FunctionName,
                    Data = message.Data,
                    Sender = new UserViewModel
                    {
                        FirstName = agent.Name,
                        Role = message.Role,
                    },
                    RichContent = message.SecondaryRichContent ?? message.RichContent
                });
            }
        }

        return dialogs;
    }

    [HttpGet("/conversation/{conversationId}")]
    public async Task<ConversationViewModel> GetConversation([FromRoute] string conversationId)
    {
        var service = _services.GetRequiredService<IConversationService>();
        var conversations = await service.GetConversations(new ConversationFilter
        {
            Id = conversationId
        });

        var userService = _services.GetRequiredService<IUserService>();
        var result = ConversationViewModel.FromSession(conversations.Items.First());

        var state = _services.GetRequiredService<IConversationStateService>();
        result.States = state.Load(conversationId, isReadOnly: true);

        var user = await userService.GetUser(result.User.Id);
        result.User = UserViewModel.FromUser(user);

        return result;
    }

    [HttpDelete("/conversation/{conversationId}")]
    public async Task<bool> DeleteConversation([FromRoute] string conversationId)
    {
        var conversationService = _services.GetRequiredService<IConversationService>();
        var response = await conversationService.DeleteConversations(new List<string> { conversationId });
        return response;
    }

    [HttpDelete("/conversation/{conversationId}/message/{messageId}")]
    public async Task<bool> DeleteConversationMessage([FromRoute] string conversationId, [FromRoute] string messageId)
    {
        var conversationService = _services.GetRequiredService<IConversationService>();
        var response = await conversationService.TruncateConversation(conversationId, messageId);
        return response;
    }

    [HttpPost("/conversation/{agentId}/{conversationId}")]
    public async Task<ChatResponseModel> SendMessage([FromRoute] string agentId,
        [FromRoute] string conversationId,
        [FromBody] NewMessageModel input)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        if (!string.IsNullOrEmpty(input.TruncateMessageId))
        {
            await conv.TruncateConversation(conversationId, input.TruncateMessageId);
        }

        var inputMsg = new RoleDialogModel(AgentRole.User, input.Text);
        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.SetMessageId(conversationId, inputMsg.MessageId);

        conv.SetConversationId(conversationId, input.States);
        conv.States.SetState("channel", input.Channel, source: StateSource.External)
                   .SetState("provider", input.Provider, source: StateSource.External)
                   .SetState("model", input.Model, source: StateSource.External)
                   .SetState("temperature", input.Temperature, source: StateSource.External)
                   .SetState("sampling_factor", input.SamplingFactor, source: StateSource.External);

        var response = new ChatResponseModel();
        
        await conv.SendMessage(agentId, inputMsg,
            replyMessage: input.Postback,
            async msg =>
            {
                response.Text = !string.IsNullOrEmpty(msg.SecondaryContent) ? msg.SecondaryContent : msg.Content;
                response.Function = msg.FunctionName;
                response.RichContent = msg.SecondaryRichContent ?? msg.RichContent;
                response.Instruction = msg.Instruction;
                response.Data = msg.Data;
            },
            _ => Task.CompletedTask,
            _ => Task.CompletedTask);

        var state = _services.GetRequiredService<IConversationStateService>();
        response.States = state.GetStates();
        response.MessageId = inputMsg.MessageId;
        response.ConversationId = conversationId;

        return response;
    }

    [HttpPost("/conversation/{conversationId}/attachments")]
    public IActionResult UploadAttachments([FromRoute] string conversationId, 
        IFormFile[] files)
    {
        if (files != null && files.Length > 0)
        {
            var attachmentService = _services.GetRequiredService<IConversationAttachmentService>();
            var dir = attachmentService.GetDirectory(conversationId);
            foreach (var file in files)
            {
                // Save the file, process it, etc.
                var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                var filePath = Path.Combine(dir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
            }

            return Ok(new { message = "File uploaded successfully." });
        }

        return BadRequest(new { message = "Invalid file." });
    }
}
