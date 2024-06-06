using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Users.Enums;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class ConversationController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConversationController(IServiceProvider services,
        IUserIdentity user,
        BotSharpOptions options)
    {
        _services = services;
        _user = user;
        _jsonOptions = InitJsonOptions(options);

    }

    [HttpPost("/conversation/{agentId}")]
    public async Task<ConversationViewModel> NewConversation([FromRoute] string agentId, [FromBody] MessageConfig config)
    {
        var service = _services.GetRequiredService<IConversationService>();
        var conv = new Conversation
        {
            AgentId = agentId,
            Channel = ConversationChannel.OpenAPI,
            TaskId = config.TaskId
        };
        conv = await service.NewConversation(conv);
        service.SetConversationId(conv.Id, config.States);

        return ConversationViewModel.FromSession(conv);
    }

    [HttpPost("/conversations")]
    public async Task<PagedItems<ConversationViewModel>> GetConversations([FromBody] ConversationFilter filter)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var userService = _services.GetRequiredService<IUserService>();
        var user = await userService.GetUser(_user.Id);
        if (user == null)
        {
            return new PagedItems<ConversationViewModel>();
        }

        filter.UserId = user.Role != UserRole.Admin ? user.Id : null;
        var conversations = await convService.GetConversations(filter);
        var agentService = _services.GetRequiredService<IAgentService>();
        var list = conversations.Items.Select(x => ConversationViewModel.FromSession(x)).ToList();

        foreach (var item in list)
        {
            user = await userService.GetUser(item.User.Id);
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
                    Sender = UserViewModel.FromUser(user),
                    Payload = message.Payload
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
    public async Task<ConversationViewModel?> GetConversation([FromRoute] string conversationId)
    {
        var service = _services.GetRequiredService<IConversationService>();
        var userService = _services.GetRequiredService<IUserService>();
        var user = await userService.GetUser(_user.Id);
        if (user == null)
        {
            return null;
        }

        var filter = new ConversationFilter
        {
            Id = conversationId,
            UserId = user.Role != UserRole.Admin ? user.Id : null
        };
        var conversations = await service.GetConversations(filter);
        if (conversations.Items.IsNullOrEmpty())
        {
            return null;
        }
        
        var result = ConversationViewModel.FromSession(conversations.Items.First());
        var state = _services.GetRequiredService<IConversationStateService>();
        result.States = state.Load(conversationId, isReadOnly: true);
        result.User = UserViewModel.FromUser(user);

        return result;
    }

    [HttpPost("/conversation/summary")]
    public async Task<string> GetConversationSummary([FromBody] ConversationSummaryModel input)
    {
        var service = _services.GetRequiredService<IConversationService>();
        return await service.GetConversationSummary(input.ConversationIds);
    }

    [HttpGet("/conversation/{conversationId}/user")]
    public async Task<UserViewModel> GetConversationUser([FromRoute] string conversationId)
    {
        var service = _services.GetRequiredService<IConversationService>();
        var conversations = await service.GetConversations(new ConversationFilter
        {
            Id = conversationId
        });

        var userService = _services.GetRequiredService<IUserService>();
        var conversation = conversations?.Items?.FirstOrDefault();
        var userId = conversation == null ? _user.Id : conversation.UserId;
        var user = await userService.GetUser(userId);
        if (user == null)
        {
            return new UserViewModel
            {
                Id = _user.Id,
                UserName = _user.UserName,
                FirstName = _user.FirstName,
                LastName = _user.LastName,
                Email = _user.Email,
                Source = "Unknown"
            };
        }

        return UserViewModel.FromUser(user);
    }

    [HttpDelete("/conversation/{conversationId}")]
    public async Task<bool> DeleteConversation([FromRoute] string conversationId)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var conversationService = _services.GetRequiredService<IConversationService>();

        var user = await userService.GetUser(_user.Id);
        var filter = new ConversationFilter
        {
            Id = conversationId,
            UserId = user.Role != UserRole.Admin ? user.Id : null
        };
        var conversations = await conversationService.GetConversations(filter);

        if (conversations.Items.IsNullOrEmpty())
        {
            return false;
        }

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

    #region Send message
    [HttpPost("/conversation/{agentId}/{conversationId}")]
    public async Task<ChatResponseModel> SendMessage([FromRoute] string agentId,
        [FromRoute] string conversationId,
        [FromBody] NewMessageModel input)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var inputMsg = new RoleDialogModel(AgentRole.User, input.Text)
        {
            Files = input.Files,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(input.TruncateMessageId))
        {
            await conv.TruncateConversation(conversationId, input.TruncateMessageId, inputMsg.MessageId);
        }

        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.SetMessageId(conversationId, inputMsg.MessageId);

        conv.SetConversationId(conversationId, input.States);
        SetStates(conv, input);

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

    [HttpPost("/conversation/{agentId}/{conversationId}/sse")]
    public async Task SendMessageSse([FromRoute] string agentId,
        [FromRoute] string conversationId,
        [FromBody] NewMessageModel input)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var inputMsg = new RoleDialogModel(AgentRole.User, input.Text)
        {
            Files = input.Files,
            CreatedAt = DateTime.UtcNow
        };

        if (!string.IsNullOrEmpty(input.TruncateMessageId))
        {
            await conv.TruncateConversation(conversationId, input.TruncateMessageId, inputMsg.MessageId);
        }

        var state = _services.GetRequiredService<IConversationStateService>();

        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.SetMessageId(conversationId, inputMsg.MessageId);

        conv.SetConversationId(conversationId, input.States);
        SetStates(conv, input);

        var response = new ChatResponseModel
        {
            ConversationId = conversationId,
            MessageId = inputMsg.MessageId,
        };

        Response.StatusCode = 200;
        Response.Headers.Append(Microsoft.Net.Http.Headers.HeaderNames.ContentType, "text/event-stream");
        Response.Headers.Append(Microsoft.Net.Http.Headers.HeaderNames.CacheControl, "no-cache");
        Response.Headers.Append(Microsoft.Net.Http.Headers.HeaderNames.Connection, "keep-alive");

        await conv.SendMessage(agentId, inputMsg,
            replyMessage: input.Postback,
            // responsed generated
            async msg =>
            {
                response.Text = !string.IsNullOrEmpty(msg.SecondaryContent) ? msg.SecondaryContent : msg.Content;
                response.Function = msg.FunctionName;
                response.RichContent = msg.SecondaryRichContent ?? msg.RichContent;
                response.Instruction = msg.Instruction;
                response.Data = msg.Data;
                response.States = state.GetStates();

                await OnChunkReceived(Response, response);
            },
            // executing
            async msg =>
            {
                var indicator = new ChatResponseModel
                {
                    ConversationId = conversationId,
                    MessageId = msg.MessageId,
                    Text = msg.Indication, 
                    Function = "indicating",
                    Instruction = msg.Instruction,
                    States = new Dictionary<string, string>()
                };
                await OnChunkReceived(Response, indicator);
            },
            // executed
            async msg =>
            {

            });

        response.States = state.GetStates();
        response.MessageId = inputMsg.MessageId;
        response.ConversationId = conversationId;

        // await OnEventCompleted(Response);
    }
    #endregion

    #region Files and attachments
    [HttpPost("/conversation/{conversationId}/attachments")]
    public IActionResult UploadAttachments([FromRoute] string conversationId,
        IFormFile[] files)
    {
        if (files != null && files.Length > 0)
        {
            var fileService = _services.GetRequiredService<IBotSharpFileService>();
            var dir = fileService.GetDirectory(conversationId);
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

    [HttpGet("/conversation/{conversationId}/files/{messageId}")]
    public IEnumerable<MessageFileViewModel> GetMessageFiles([FromRoute] string conversationId, [FromRoute] string messageId)
    {
        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        var files = fileService.GetMessageFiles(conversationId, new List<string> { messageId }, imageOnly: true);
        return files?.Select(x => MessageFileViewModel.Transform(x))?.ToList() ?? new List<MessageFileViewModel>();
    }

    [HttpGet("/conversation/{conversationId}/message/{messageId}/file/{fileName}")]
    public IActionResult GetMessageFile([FromRoute] string conversationId, [FromRoute] string messageId, [FromRoute] string fileName)
    {
        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        var file = fileService.GetMessageFile(conversationId, messageId, fileName);
        if (string.IsNullOrEmpty(file))
        {
            return NotFound();
        }
        return BuildFileResult(file);
    }
    #endregion

    #region Private methods
    private void SetStates(IConversationService conv, NewMessageModel input)
    {
        conv.States.SetState("channel", input.Channel, source: StateSource.External)
           .SetState("provider", input.Provider, source: StateSource.External)
           .SetState("model", input.Model, source: StateSource.External)
           .SetState("temperature", input.Temperature, source: StateSource.External)
           .SetState("sampling_factor", input.SamplingFactor, source: StateSource.External);
    }

    private FileContentResult BuildFileResult(string file)
    {
        using Stream stream = System.IO.File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, (int)stream.Length);
        return File(bytes, "application/octet-stream", Path.GetFileName(file));
    }

    private async Task OnChunkReceived(HttpResponse response, ChatResponseModel message)
    {
        var json = JsonSerializer.Serialize(message, _jsonOptions);

        var buffer = Encoding.UTF8.GetBytes($"data:{json}\n");
        await response.Body.WriteAsync(buffer, 0, buffer.Length);
        await Task.Delay(10);

        buffer = Encoding.UTF8.GetBytes("\n");
        await response.Body.WriteAsync(buffer, 0, buffer.Length);
    }

    private async Task OnEventCompleted(HttpResponse response)
    {
        var buffer = Encoding.UTF8.GetBytes("data:[DONE]\n");
        await response.Body.WriteAsync(buffer, 0, buffer.Length);

        buffer = Encoding.UTF8.GetBytes("\n");
        await response.Body.WriteAsync(buffer, 0, buffer.Length);
    }

    private JsonSerializerOptions InitJsonOptions(BotSharpOptions options)
    {
        var jsonOption = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            AllowTrailingCommas = true
        };

        if (options?.JsonSerializerOptions != null)
        {
            foreach (var option in options.JsonSerializerOptions.Converters)
            {
                jsonOption.Converters.Add(option);
            }
        }

        return jsonOption;
    }
    #endregion
}
