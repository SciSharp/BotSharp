using BotSharp.Abstraction.ApiAdapters;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Models;
using BotSharp.OpenAPI.ViewModels.Conversations;
using BotSharp.OpenAPI.ViewModels.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic;
using System.Net.Http.Headers;

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
            AgentId = agentId,
            UserId = _user.Id
        };
        conv = await service.NewConversation(conv);
        config.States.ForEach(x => conv.States[x.Split('=')[0]] = x.Split('=')[1]);

        return ConversationViewModel.FromSession(conv);
    }

    [HttpGet("/conversations/{agentId}")]
    public async Task<IEnumerable<ConversationViewModel>> GetConversations()
    {
        var service = _services.GetRequiredService<IConversationService>();
        var conversations = await service.GetConversations();
        var userService = _services.GetRequiredService<IUserService>();
        var list = conversations.Select(x => new ConversationViewModel
        {
            Id = x.Id,
            AgentId = x.AgentId,
            Title = x.Title,
            User = new UserViewModel
            {
                Id = x.UserId,
            },
            CreatedTime = x.CreatedTime,
            UpdatedTime = x.UpdatedTime
        }).ToList();

        foreach (var item in list)
        {
            var user = await userService.GetUser(item.User.Id);
            item.User = UserViewModel.FromUser(user);
        }

        return list;
    }

    [HttpGet("/conversation/{conversationId}/dialogs")]
    public async Task<IEnumerable<ChatResponseModel>> GetDialogs([FromRoute] string conversationId)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(conversationId, new List<string>());
        var history = conv.GetDialogHistory();

        var userService = _services.GetRequiredService<IUserService>();

        var dialogs = new List<ChatResponseModel>();
        foreach (var message in history)
        {
            var user = await userService.GetUser(message.SenderId);

            dialogs.Add(new ChatResponseModel
            {
                ConversationId = conversationId,
                MessageId = message.MessageId,
                CreatedAt = message.CreatedAt,
                Text = message.Content,
                Sender = UserViewModel.FromUser(user)
            });
        }

        return dialogs;
    }

    [HttpDelete("/conversation/{conversationId}")]
    public async Task DeleteConversation([FromRoute] string conversationId)
    {
        var service = _services.GetRequiredService<IConversationService>();
    }

    [HttpPost("/conversation/{agentId}/{conversationId}")]
    public async Task<ChatResponseModel> SendMessage([FromRoute] string agentId,
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

        var response = new ChatResponseModel();
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
