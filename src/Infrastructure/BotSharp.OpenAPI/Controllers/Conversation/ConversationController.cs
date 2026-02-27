using BotSharp.Abstraction.Files.Constants;
using BotSharp.Abstraction.Files.Enums;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.MessageHub.Services;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Repositories;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Users.Dtos;
using BotSharp.Core.Infrastructures;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public partial class ConversationController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly IUserIdentity _user;
    private readonly JsonSerializerOptions _jsonOptions;

    public ConversationController(
        IServiceProvider services,
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
        var channel = config.States.FirstOrDefault(x => x.Key == "channel");
        var conv = new Conversation
        {
            AgentId = agentId,
            Channel = channel == default ? ConversationChannel.OpenAPI : channel.Value.ToString(),
            Tags = config.Tags ?? new(),
            TaskId = config.TaskId
        };
        conv = await service.NewConversation(conv);
        await service.SetConversationId(conv.Id, config.States);

        return ConversationViewModel.FromSession(conv);
    }

    [HttpGet("/conversations")]
    public async Task<PagedItems<ConversationViewModel>> GetConversations([FromQuery] ConversationFilter filter)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var userService = _services.GetRequiredService<IUserService>();
        var (isAdmin, user) = await userService.IsAdminUser(_user.Id);
        if (user == null)
        {
            return new PagedItems<ConversationViewModel>();
        }

        filter.UserId = !isAdmin ? user.Id : filter.UserId;
        var conversations = await convService.GetConversations(filter);
        var agentService = _services.GetRequiredService<IAgentService>();
        var list = conversations.Items.Select(x => ConversationViewModel.FromSession(x)).ToList();

        var agentIds = list.Select(x => x.AgentId).ToList();
        var agents = await agentService.GetAgentOptions(agentIds);

        var userIds = list.Select(x => x.User.Id).ToList();
        var users = await userService.GetUsers(userIds);

        var files = new List<ConversationFile>();
        if (filter.IsLoadThumbnail)
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            files = await db.GetConversationFiles(new ConversationFileFilter
            {
                ConversationIds = list.Select(x => x.Id)
            });
        }

        foreach (var item in list)
        {
            user = users.FirstOrDefault(x => x.Id == item.User.Id);
            item.User = UserViewModel.FromUser(user);
            var agent = agents.FirstOrDefault(x => x.Id == item.AgentId);
            item.AgentName = agent?.Name ?? "Unkown";
            item.Thumbnail = !files.IsNullOrEmpty() ? files.FirstOrDefault(x => x.ConversationId == item.Id)?.Thumbnail : null;
        }

        return new PagedItems<ConversationViewModel>
        {
            Count = conversations.Count,
            Items = list
        };
    }

    [HttpGet("/conversation/{conversationId}/dialogs")]
    public async Task<IEnumerable<ChatResponseModel>> GetDialogs(
        [FromRoute] string conversationId,
        [FromQuery] int count = 100,
        [FromQuery] string order = "asc")
    {
        var conv = _services.GetRequiredService<IConversationService>();
        await conv.SetConversationId(conversationId, [], isReadOnly: true);
        var history = await conv.GetDialogHistory(lastCount: count, fromBreakpoint: false, filter: new()
        {
            Order = order
        });

        var userService = _services.GetRequiredService<IUserService>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();

        var messageIds = history.Select(x => x.MessageId).Distinct().ToList();
        var files = fileStorage.GetMessageFiles(conversationId, messageIds, options: new() { Sources = [FileSource.User, FileSource.Bot] });

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
                    MessageLabel = message.MessageLabel,
                    CreatedAt = message.CreatedAt,
                    Text = !string.IsNullOrEmpty(message.SecondaryContent) ? message.SecondaryContent : message.Content,
                    Data = message.Data,
                    Sender = UserDto.FromUser(user),
                    Payload = message.Payload,
                    HasMessageFiles = files.Any(x => x.MessageId.IsEqualTo(message.MessageId) && x.FileSource == FileSource.User)
                });
            }
            else if (message.Role == AgentRole.Assistant)
            {
                var agent = await agentService.GetAgent(message.CurrentAgentId);
                dialogs.Add(new ChatResponseModel
                {
                    ConversationId = conversationId,
                    MessageId = message.MessageId,
                    MessageLabel = message.MessageLabel,
                    CreatedAt = message.CreatedAt,
                    Text = !string.IsNullOrEmpty(message.SecondaryContent) ? message.SecondaryContent : message.Content,
                    Function = message.FunctionName,
                    Data = message.Data,
                    Sender = new()
                    {
                        FirstName = agent?.Name ?? "Unkown",
                        Role = message.Role,
                    },
                    RichContent = message.SecondaryRichContent ?? message.RichContent,
                    HasMessageFiles = files.Any(x => x.MessageId.IsEqualTo(message.MessageId) && x.FileSource == FileSource.Bot)
                });
            }
        }
        return dialogs;
    }

    [HttpGet("/conversation/{conversationId}")]
    public async Task<ConversationViewModel?> GetConversation(
        [FromRoute] string conversationId,
        [FromQuery] bool isLoadStates = false,
        [FromQuery] bool isLoadThumbnail = false)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var userService = _services.GetRequiredService<IUserService>();
        var settings = _services.GetRequiredService<PluginSettings>();

        var (isAdmin, user) = await userService.IsAdminUser(_user.Id);

        var filter = new ConversationFilter
        {
            Id = conversationId,
            UserId = !isAdmin ? user?.Id : null,
            IsLoadLatestStates = isLoadStates
        };

        var conversations = await convService.GetConversations(filter);
        var conversation = conversations.Items?.FirstOrDefault();
        if (conversation == null)
        {
            return new();
        }

        user = !string.IsNullOrEmpty(conversation.UserId)
                ? await userService.GetUser(conversation.UserId)
                : null;

        if (user == null)
        {
            user = new User
            {
                Id = _user.Id,
                UserName = _user.UserName,
                FirstName = _user.FirstName,
                LastName = _user.LastName,
                Email = _user.Email,
                Source = "Unknown"
            };
        }

        var conversationView = ConversationViewModel.FromSession(conversation);
        conversationView.User = UserViewModel.FromUser(user);
        conversationView.IsRealtimeEnabled = settings?.Assemblies?.Contains("BotSharp.Core.Realtime") ?? false;

        if (isLoadThumbnail)
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var files = await db.GetConversationFiles(new ConversationFileFilter
            {
                ConversationIds = [conversation.Id]
            });
            conversationView.Thumbnail = files?.FirstOrDefault()?.Thumbnail;
        }

        return conversationView;
    }

    [HttpPost("/conversation/summary")]
    public async Task<string> GetConversationSummary([FromBody] ConversationSummaryModel input)
    {
        var service = _services.GetRequiredService<IConversationService>();
        return await service.GetConversationSummary(input);
    }

    [HttpPut("/conversation/{conversationId}/update-title")]
    public async Task<bool> UpdateConversationTitle([FromRoute] string conversationId, [FromBody] UpdateConversationTitleModel newTile)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var conv = _services.GetRequiredService<IConversationService>();

        var (isAdmin, user) = await userService.IsAdminUser(_user.Id);
        var filter = new ConversationFilter
        {
            Id = conversationId,
            UserId = !isAdmin ? user?.Id : null
        };
        var conversations = await conv.GetConversations(filter);

        if (conversations.Items.IsNullOrEmpty())
        {
            return false;
        }

        await conv.UpdateConversationTitle(conversationId, newTile.NewTitle);
        return true;
    }

    [HttpPut("/conversation/{conversationId}/update-title-alias")]
    public async Task<bool> UpdateConversationTitleAlias([FromRoute] string conversationId, [FromBody] UpdateConversationTitleAliasModel newTile)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var conversationService = _services.GetRequiredService<IConversationService>();

        var (isAdmin, user) = await userService.IsAdminUser(_user.Id);
        var filter = new ConversationFilter
        {
            Id = conversationId,
            UserId = !isAdmin ? user?.Id : null
        };
        var conversations = await conversationService.GetConversations(filter);

        if (conversations.Items.IsNullOrEmpty())
        {
            return false;
        }

        var response = await conversationService.UpdateConversationTitleAlias(conversationId, newTile.NewTitleAlias);
        return response != null;
    }


    [HttpPut("/conversation/{conversationId}/update-tags")]
    public async Task<bool> UpdateConversationTags([FromRoute] string conversationId, [FromBody] UpdateConversationRequest request)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        return await conv.UpdateConversationTags(conversationId, request.ToAddTags, request.ToDeleteTags);
    }

    [HttpPut("/conversation/{conversationId}/update-message")]
    public async Task<bool> UpdateConversationMessage([FromRoute] string conversationId, [FromBody] UpdateMessageModel model)
    {
        var conversationService = _services.GetRequiredService<IConversationService>();
        var request = new UpdateMessageRequest
        {
            Message = new DialogElement
            {
                MetaData = new DialogMetaData
                {
                    MessageId = model.Message.MessageId,
                    Role = model.Message.Sender?.Role
                },
                Content = model.Message.Text,
                RichContent = JsonSerializer.Serialize(model.Message.RichContent, _jsonOptions),
            },
            InnerIndex = model.InnerIndex
        };

        return await conversationService.UpdateConversationMessage(conversationId, request);
    }


    [HttpDelete("/conversation/{conversationId}")]
    public async Task<bool> DeleteConversation([FromRoute] string conversationId)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var conversationService = _services.GetRequiredService<IConversationService>();

        var (isAdmin, user) = await userService.IsAdminUser(_user.Id);
        var filter = new ConversationFilter
        {
            Id = conversationId,
            UserId = !isAdmin ? user?.Id : null
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
    public async Task<IActionResult> DeleteConversationMessage([FromRoute] string conversationId, [FromRoute] string messageId, [FromBody] TruncateMessageRequest request)
    {
        var conversationService = _services.GetRequiredService<IConversationService>();
        var newMessageId = request.isNewMessage ? Guid.NewGuid().ToString() : null;
        var isSuccess = await conversationService.TruncateConversation(conversationId, messageId, newMessageId);
        return Ok(new { Deleted = isSuccess, MessageId = isSuccess ? newMessageId : string.Empty });
    }

    #region Send notification
    [HttpPost("/conversation/{conversationId}/notification")]
    public async Task<ChatResponseModel> SendNotification([FromRoute] string conversationId, [FromBody] NewMessageModel input)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var routing = _services.GetRequiredService<IRoutingService>();
        var userService = _services.GetRequiredService<IUserService>();

        await conv.SetConversationId(conversationId, new List<MessageState>(), isReadOnly: true);

        var inputMsg = new RoleDialogModel(AgentRole.User, input.Text)
        {
            MessageId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        var user = await userService.GetUser(_user.Id);
        var response = new ChatResponseModel()
        {
            ConversationId = conversationId,
            MessageId = inputMsg.MessageId,
            Sender = new UserViewModel
            {
                Id = user?.Id ?? string.Empty,
                FirstName = user?.FirstName ?? string.Empty,
                LastName = user?.LastName ?? string.Empty
            },
            CreatedAt = DateTime.UtcNow
        };

        await HookEmitter.Emit<IConversationHook>(_services, async hook =>
            await hook.OnNotificationGenerated(inputMsg),
            routing.Context.GetCurrentAgentId()
        );

        return response;
    }
    #endregion

    #region Send message
    [HttpPost("/conversation/{agentId}/{conversationId}")]
    public async Task<ChatResponseModel> SendMessage(
        [FromRoute] string agentId,
        [FromRoute] string conversationId,
        [FromBody] NewMessageModel input)
    {
        var observer = _services.GetRequiredService<IObserverService>();
        using var container = observer.SubscribeObservers<HubObserveData<RoleDialogModel>>(conversationId);

        var conv = _services.GetRequiredService<IConversationService>();
        var inputMsg = new RoleDialogModel(AgentRole.User, input.Text)
        {
            MessageId = !string.IsNullOrWhiteSpace(input.InputMessageId) ? input.InputMessageId : Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.SetMessageId(conversationId, inputMsg.MessageId);

        await conv.SetConversationId(conversationId, input.States);
        SetStates(conv, input);

        var response = new ChatResponseModel();
        await conv.SendMessage(agentId, inputMsg,
            replyMessage: input.Postback,
            async msg =>
            {
                response.Text = !string.IsNullOrEmpty(msg.SecondaryContent) ? msg.SecondaryContent : msg.Content;
                response.Function = msg.FunctionName;
                response.MessageLabel = msg.MessageLabel;
                response.RichContent = msg.SecondaryRichContent ?? msg.RichContent;
                response.Instruction = msg.Instruction;
                response.Data = msg.Data;
            });

        var state = _services.GetRequiredService<IConversationStateService>();
        response.States = state.GetStates();
        response.MessageId = inputMsg.MessageId;
        response.ConversationId = conversationId;

        return response;
    }


    [HttpPost("/conversation/{agentId}/{conversationId}/sse")]
    public async Task SendMessageSse([FromRoute] string agentId, [FromRoute] string conversationId, [FromBody] NewMessageModel input)
    {
        var observer = _services.GetRequiredService<IObserverService>();
        using var container = observer.SubscribeObservers<HubObserveData<RoleDialogModel>>(conversationId, listeners: new()
        {
            { ChatEvent.OnIndicationReceived, async data => await OnReceiveToolCallIndication(conversationId, data.Data) }
        });

        var conv = _services.GetRequiredService<IConversationService>();
        var inputMsg = new RoleDialogModel(AgentRole.User, input.Text)
        {
            MessageId = !string.IsNullOrWhiteSpace(input.InputMessageId) ? input.InputMessageId : Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        var state = _services.GetRequiredService<IConversationStateService>();

        var routing = _services.GetRequiredService<IRoutingService>();
        routing.Context.SetMessageId(conversationId, inputMsg.MessageId);

        await conv.SetConversationId(conversationId, input.States);
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
                response.MessageLabel = msg.MessageLabel;
                response.Function = msg.FunctionName;
                response.RichContent = msg.SecondaryRichContent ?? msg.RichContent;
                response.Instruction = msg.Instruction;
                response.Data = msg.Data;
                response.States = state.GetStates();
                
                await OnChunkReceived(Response, response);
            });

        response.States = state.GetStates();
        response.MessageId = inputMsg.MessageId;
        response.ConversationId = conversationId;

        // await OnEventCompleted(Response);
    }

    private async Task OnReceiveToolCallIndication(string conversationId, RoleDialogModel msg)
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
    }
    #endregion

    #region Private methods
    private void SetStates(IConversationService conv, NewMessageModel input)
    {
        if (string.IsNullOrEmpty(conv.States.GetState("channel")))
        {
            conv.States.SetState("channel", input.Channel, source: StateSource.External);
        }
        if (string.IsNullOrEmpty(conv.States.GetState("provider")))
        {
            conv.States.SetState("provider", input.Provider, source: StateSource.External);
        }
        if (string.IsNullOrEmpty(conv.States.GetState("model")))
        {
            conv.States.SetState("model", input.Model, source: StateSource.External);
        }
        if (string.IsNullOrEmpty(conv.States.GetState("temperature")))
        {
            conv.States.SetState("temperature", input.Temperature, source: StateSource.External);
        }
        if (string.IsNullOrEmpty(conv.States.GetState("sampling_factor")))
        {
            conv.States.SetState("sampling_factor", input.SamplingFactor, source: StateSource.External);
        }
    }

    private FileContentResult BuildFileResult(string file)
    {
        using Stream stream = System.IO.File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, (int)stream.Length);
        var fileExtension = Path.GetExtension(file).ToLower();
        var enableRangeProcessing = FileConstants.AudioExtensions.Contains(fileExtension);
        return File(bytes, "application/octet-stream", Path.GetFileName(file), enableRangeProcessing: enableRangeProcessing);
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