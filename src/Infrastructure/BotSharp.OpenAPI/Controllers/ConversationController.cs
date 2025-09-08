using BotSharp.Abstraction.Chart;
using BotSharp.Abstraction.Files.Constants;
using BotSharp.Abstraction.Files.Enums;
using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Abstraction.MessageHub.Services;
using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Routing;
using BotSharp.Abstraction.Users.Dtos;
using BotSharp.Core.Infrastructures;

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
        var channel = config.States.FirstOrDefault(x => x.Key == "channel");
        var conv = new Conversation
        {
            AgentId = agentId,
            Channel = channel == default ? ConversationChannel.OpenAPI : channel.Value.ToString(),
            Tags = config.Tags ?? new(),
            TaskId = config.TaskId
        };
        conv = await service.NewConversation(conv);
        service.SetConversationId(conv.Id, config.States);

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

        foreach (var item in list)
        {
            user = users.FirstOrDefault(x => x.Id == item.User.Id);
            item.User = UserViewModel.FromUser(user);
            var agent = agents.FirstOrDefault(x => x.Id == item.AgentId);
            item.AgentName = agent?.Name ?? "Unkown";
        }

        return new PagedItems<ConversationViewModel>
        {
            Count = conversations.Count,
            Items = list
        };
    }

    [HttpGet("/conversation/{conversationId}/dialogs")]
    public async Task<IEnumerable<ChatResponseModel>> GetDialogs([FromRoute] string conversationId, [FromQuery] int count = 100)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(conversationId, [], isReadOnly: true);
        var history = conv.GetDialogHistory(lastCount: count, fromBreakpoint: false);

        var userService = _services.GetRequiredService<IUserService>();
        var agentService = _services.GetRequiredService<IAgentService>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();

        var messageIds = history.Select(x => x.MessageId).Distinct().ToList();
        var fileMessages = fileStorage.GetMessagesWithFile(conversationId, messageIds);

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
                    HasMessageFiles = fileMessages.Any(x => x.MessageId.IsEqualTo(message.MessageId) && x.FileSource == FileSourceType.User)
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
                    HasMessageFiles = fileMessages.Any(x => x.MessageId.IsEqualTo(message.MessageId) && x.FileSource == FileSourceType.Bot)
                });
            }
        }
        return dialogs;
    }

    [HttpGet("/conversation/{conversationId}")]
    public async Task<ConversationViewModel?> GetConversation([FromRoute] string conversationId, [FromQuery] bool isLoadStates = false)
    {
        var service = _services.GetRequiredService<IConversationService>();
        var userService = _services.GetRequiredService<IUserService>();
        var settings = _services.GetRequiredService<PluginSettings>();

        var (isAdmin, user) = await userService.IsAdminUser(_user.Id);

        var filter = new ConversationFilter
        {
            Id = conversationId,
            UserId = !isAdmin ? user?.Id : null,
            IsLoadLatestStates = isLoadStates
        };

        var conversations = await service.GetConversations(filter);
        var conv = !conversations.Items.IsNullOrEmpty()
                ? ConversationViewModel.FromSession(conversations.Items.First())
                : new();

        user = !string.IsNullOrEmpty(conv?.User?.Id)
                ? await userService.GetUser(conv.User.Id)
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

        conv.User = UserViewModel.FromUser(user);
        conv.IsRealtimeEnabled = settings?.Assemblies?.Contains("BotSharp.Core.Realtime") ?? false;
        return conv;
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

        var response = await conv.UpdateConversationTitle(conversationId, newTile.NewTitle);
        return response != null;
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
            InnderIndex = model.InnerIndex
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
    public async Task<string?> DeleteConversationMessage([FromRoute] string conversationId, [FromRoute] string messageId, [FromBody] TruncateMessageRequest request)
    {
        var conversationService = _services.GetRequiredService<IConversationService>();
        var newMessageId = request.isNewMessage ? Guid.NewGuid().ToString() : null;
        var isSuccess = await conversationService.TruncateConversation(conversationId, messageId, newMessageId);
        return isSuccess ? newMessageId : string.Empty;
    }

    #region Send notification
    [HttpPost("/conversation/{conversationId}/notification")]
    public async Task<ChatResponseModel> SendNotification([FromRoute] string conversationId, [FromBody] NewMessageModel input)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var routing = _services.GetRequiredService<IRoutingService>();
        var userService = _services.GetRequiredService<IUserService>();

        conv.SetConversationId(conversationId, new List<MessageState>(), isReadOnly: true);

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

    #region Files and attachments
    [HttpPost("/conversation/{conversationId}/attachments")]
    public IActionResult UploadAttachments([FromRoute] string conversationId, IFormFile[] files)
    {
        if (files != null && files.Length > 0)
        {
            var fileStorage = _services.GetRequiredService<IFileStorageService>();
            var dir = fileStorage.GetDirectory(conversationId);
            foreach (var file in files)
            {
                // Save the file, process it, etc.
                var fileName = ContentDispositionHeaderValue.Parse(file.ContentDisposition).FileName.Trim('"');
                var filePath = Path.Combine(dir, fileName);

                fileStorage.SaveFileStreamToPath(filePath, file.OpenReadStream());
            }

            return Ok(new { message = "File uploaded successfully." });
        }

        return BadRequest(new { message = "Invalid file." });
    }

    [HttpPost("/agent/{agentId}/conversation/{conversationId}/upload")]
    public async Task<string> UploadConversationMessageFiles([FromRoute] string agentId, [FromRoute] string conversationId, [FromBody] InputMessageFiles input)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        convService.SetConversationId(conversationId, input.States);
        var conv = await convService.GetConversationRecordOrCreateNew(agentId);
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var messageId = Guid.NewGuid().ToString();
        var isSaved = fileStorage.SaveMessageFiles(conv.Id, messageId, FileSourceType.User, input.Files);
        return isSaved ? messageId : string.Empty;
    }

    [HttpGet("/conversation/{conversationId}/files/{messageId}/{source}")]
    public IEnumerable<MessageFileViewModel> GetConversationMessageFiles([FromRoute] string conversationId, [FromRoute] string messageId, [FromRoute] string source)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var files = fileStorage.GetMessageFiles(conversationId, new List<string> { messageId }, source);
        return files?.Select(x => MessageFileViewModel.Transform(x))?.ToList() ?? new List<MessageFileViewModel>();
    }

    [HttpGet("/conversation/{conversationId}/message/{messageId}/{source}/file/{index}/{fileName}")]
    public IActionResult GetMessageFile([FromRoute] string conversationId, [FromRoute] string messageId, [FromRoute] string source, [FromRoute] string index, [FromRoute] string fileName)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var file = fileStorage.GetMessageFile(conversationId, messageId, source, index, fileName);
        if (string.IsNullOrEmpty(file))
        {
            return NotFound();
        }
        return BuildFileResult(file);
    }

    [HttpGet("/conversation/{conversationId}/message/{messageId}/{source}/file/{index}/{fileName}/download")]
    public IActionResult DownloadMessageFile([FromRoute] string conversationId, [FromRoute] string messageId, [FromRoute] string source, [FromRoute] string index, [FromRoute] string fileName)
    {
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var file = fileStorage.GetMessageFile(conversationId, messageId, source, index, fileName);
        if (string.IsNullOrEmpty(file))
        {
            return NotFound();
        }

        var fName = file.Split(Path.DirectorySeparatorChar).Last();
        var contentType = FileUtility.GetFileContentType(fName);
        var stream = System.IO.File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, (int)stream.Length);
        stream.Position = 0;

        return new FileStreamResult(stream, contentType) { FileDownloadName = fName };
    }
    #endregion

    #region Chart
    [AllowAnonymous]
    [HttpGet("/conversation/{conversationId}/message/{messageId}/user/chart/data")]
    public async Task<ConversationChartDataResponse?> GetConversationChartData(
        [FromRoute] string conversationId,
        [FromRoute] string messageId,
        [FromQuery] ConversationChartDataRequest request)
    {
        var chart = _services.GetServices<IBotSharpChartService>().FirstOrDefault(x => x.Provider == request?.ChartProvider);
        if (chart == null) return null;

        var result = await chart.GetConversationChartData(conversationId, messageId, request);
        return ConversationChartDataResponse.From(result);
    }

    [HttpPost("/conversation/{conversationId}/message/{messageId}/user/chart/code")]
    public async Task<ConversationChartCodeResponse?> GetConversationChartCode(
        [FromRoute] string conversationId,
        [FromRoute] string messageId,
        [FromBody] ConversationChartCodeRequest request)
    {
        var chart = _services.GetServices<IBotSharpChartService>().FirstOrDefault(x => x.Provider == request?.ChartProvider);
        if (chart == null) return null;

        var result = await chart.GetConversationChartCode(conversationId, messageId, request);
        return ConversationChartCodeResponse.From(result);
    }
    #endregion

    #region Dashboard
    [HttpPut("/agent/{agentId}/conversation/{conversationId}/dashboard")]
    public async Task<bool> PinConversationToDashboard([FromRoute] string agentId, [FromRoute] string conversationId)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var pinned = await userService.AddDashboardConversation(conversationId);
        return pinned;
    }

    [HttpDelete("/agent/{agentId}/conversation/{conversationId}/dashboard")]
    public async Task<bool> UnpinConversationFromDashboard([FromRoute] string agentId, [FromRoute] string conversationId)
    {
        var userService = _services.GetRequiredService<IUserService>();
        var unpinned = await userService.RemoveDashboardConversation(conversationId);
        return unpinned;
    }
    #endregion

    #region Search state keys
    [HttpGet("/conversation/state/keys")]
    public async Task<List<string>> GetConversationStateKeys([FromQuery] ConversationStateKeysFilter request)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var keys = await convService.GetConversationStateSearhKeys(request);
        return keys;
    }
    #endregion

    #region Migrate Latest States
    [HttpPost("/conversation/latest-state/migrate")]
    public async Task<bool> MigrateConversationLatestStates([FromBody] MigrateLatestStateRequest request)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var res = await convService.MigrateLatestStates(request.BatchSize, request.ErrorLimit);
        return res;
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