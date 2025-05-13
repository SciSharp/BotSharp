using BotSharp.Abstraction.Options;
using BotSharp.Abstraction.Realtime.Models.Session;
using BotSharp.Core.Session;
using BotSharp.Plugin.GoogleAI.Models.Realtime;
using GenerativeAI;
using GenerativeAI.Core;
using GenerativeAI.Live;
using GenerativeAI.Live.Extensions;
using GenerativeAI.Types;
using GenerativeAI.Types.Converters;
using Google.Ai.Generativelanguage.V1Beta2;
using Google.Api;
using System;
using System.Threading;

namespace BotSharp.Plugin.GoogleAi.Providers.Realtime;

public class GoogleRealTimeProvider : IRealTimeCompletion
{
    public string Provider => "google-ai";
    public string Model => _model;

    private string _model = GoogleAIModels.Gemini2FlashExp;
    private MultiModalLiveClient _client;
    private GenerativeModel _chatClient;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private List<string> renderedInstructions = [];

    private LlmRealtimeSession _session;
    private readonly BotSharpOptions _botsharpOptions;
    private readonly GoogleAiSettings _settings;

    private const string DEFAULT_MIME_TYPE = "audio/pcm;rate=16000";

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(), new DateOnlyJsonConverter(), new TimeOnlyJsonConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnknownTypeHandling = JsonUnknownTypeHandling.JsonElement
    };

    public GoogleRealTimeProvider(
        IServiceProvider services,
        GoogleAiSettings settings,
        BotSharpOptions botSharpOptions,
        ILogger<GoogleRealTimeProvider> logger)
    {
        _settings = settings;
        _botsharpOptions = botSharpOptions;
        _services = services;
        _logger = logger;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    private RealtimeHubConnection _conn;
    private Func<Task> _onModelReady;
    private Func<string, string, Task> _onModelAudioDeltaReceived;
    private Func<Task> _onModelAudioResponseDone;
    private Func<string, Task> _onModelAudioTranscriptDone;
    private Func<List<RoleDialogModel>, Task> _onModelResponseDone;
    private Func<string, Task> _onConversationItemCreated;
    private Func<RoleDialogModel, Task> _onInputAudioTranscriptionDone;
    private Func<Task> _onUserInterrupted;
    

    public async Task Connect(
        RealtimeHubConnection conn,
        Func<Task> onModelReady,
        Func<string, string, Task> onModelAudioDeltaReceived,
        Func<Task> onModelAudioResponseDone,
        Func<string, Task> onModelAudioTranscriptDone,
        Func<List<RoleDialogModel>, Task> onModelResponseDone,
        Func<string, Task> onConversationItemCreated,
        Func<RoleDialogModel, Task> onInputAudioTranscriptionDone,
        Func<Task> onInterruptionDetected)
    {
        _conn = conn;
        _onModelReady = onModelReady;
        _onModelAudioDeltaReceived = onModelAudioDeltaReceived;
        _onModelAudioResponseDone = onModelAudioResponseDone;
        _onModelAudioTranscriptDone = onModelAudioTranscriptDone;
        _onModelResponseDone = onModelResponseDone;
        _onConversationItemCreated = onConversationItemCreated;
        _onInputAudioTranscriptionDone = onInputAudioTranscriptionDone;
        _onUserInterrupted = onInterruptionDetected;

        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var realtimeModelSettings = _services.GetRequiredService<RealtimeModelSettings>();

        _model = realtimeModelSettings.Model;
        var modelSettings = settingsService.GetSetting(Provider, _model);

        if (_session != null)
        {
            _session.Dispose();
        }

        _session = new LlmRealtimeSession(_services, new ChatSessionOptions
        {
            JsonOptions = _jsonOptions
        });

        await _session.ConnectAsync(
            uri: new Uri($"wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.v1beta.GenerativeService.BidiGenerateContent?key={modelSettings.ApiKey}"),
            cancellationToken: CancellationToken.None);

        await onModelReady();

        _ = ReceiveMessage(
            conn,
            onModelReady,
            onModelAudioDeltaReceived,
            onModelAudioResponseDone,
            onModelAudioTranscriptDone,
            onModelResponseDone,
            onConversationItemCreated,
            onInputAudioTranscriptionDone,
            onInterruptionDetected);


        //var client = ProviderHelper.GetGeminiClient(Provider, _model, _services);
        //_chatClient = client.CreateGenerativeModel(_model);
        //_client = _chatClient.CreateMultiModalLiveClient(
        //    config: new GenerationConfig
        //    {
        //        ResponseModalities = [Modality.AUDIO],
        //    },
        //    systemInstruction: "You are a helpful assistant.",
        //    logger: _logger);

        //await AttachEvents(_client);

        //await _client.ConnectAsync(false);
    }

    private async Task ReceiveMessage(
        RealtimeHubConnection conn,
        Func<Task> onModelReady,
        Func<string, string, Task> onModelAudioDeltaReceived,
        Func<Task> onModelAudioResponseDone,
        Func<string, Task> onModelAudioTranscriptDone,
        Func<List<RoleDialogModel>, Task> onModelResponseDone,
        Func<string, Task> onConversationItemCreated,
        Func<RoleDialogModel, Task> onInputAudioTranscriptionCompleted,
        Func<Task> onInterruptionDetected)
    {
        await foreach (ChatSessionUpdate update in _session.ReceiveUpdatesAsync(CancellationToken.None))
        {
            var receivedText = update?.RawResponse;
            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }

            Console.WriteLine($"Received text: {receivedText}");
            try
            {
                var response = JsonSerializer.Deserialize<RealtimeServerResponse>(receivedText, _jsonOptions);

                if (response == null)
                {
                    continue;
                }

                if (response.SetupComplete != null)
                {
                    _logger.LogInformation($"Session setup completed.");
                }
                else if (response.ServerContent != null)
                {
                    if (response.ServerContent.ModelTurn != null)
                    {
                        _logger.LogInformation($"Model audio delta received.");
                        var parts = response.ServerContent.ModelTurn.Parts;
                        if (!parts.IsNullOrEmpty())
                        {
                            foreach (var part in parts)
                            {
                                if (!string.IsNullOrEmpty(part.InlineData?.Data))
                                {
                                    await onModelAudioDeltaReceived(part.InlineData.Data, string.Empty);
                                }
                            }
                        }
                    }
                    else if (response.ServerContent.GenerationComplete == true)
                    {
                        _logger.LogInformation($"Model generation completed.");
                    }
                    else if (response.ServerContent.TurnComplete == true)
                    {
                        _logger.LogInformation($"Model turn completed.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when deserializing server response.");
                continue;
            }
        }

        _session.Dispose();
    }


    public async Task Disconnect()
    {
        if (_session != null)
        {
            await _session.Disconnect();
        }

        //if (_client != null)
        //{
        //    await _client.DisconnectAsync();
        //}
    }

    public async Task AppenAudioBuffer(string message)
    {
        //await _client.SendAudioAsync(Convert.FromBase64String(message));

        await SendEventToModel(new BidiClientPayload
        {
            RealtimeInput = new()
            {
                MediaChunks = [new() { Data = message, MimeType = DEFAULT_MIME_TYPE }]
            }
        });
    }

    public async Task AppenAudioBuffer(ArraySegment<byte> data, int length)
    {
        var buffer = data.AsSpan(0, length).ToArray();
        //await _client.SendAudioAsync(buffer, "audio/pcm;rate=16000");

        await SendEventToModel(new BidiClientPayload
        {
            RealtimeInput = new()
            {
                MediaChunks = [new() { Data = Convert.ToBase64String(buffer), MimeType = DEFAULT_MIME_TYPE }]
            }
        });
    }

    public async Task TriggerModelInference(string? instructions = null)
    {
        var content = !string.IsNullOrWhiteSpace(instructions)
                    ? new Content(instructions, AgentRole.User)
                    : null;

        //await _client.SendClientContentAsync(new BidiGenerateContentClientContent()
        //{
        //    Turns = content != null ? [content] : null,
        //    TurnComplete = true,
        //});

        await SendEventToModel(new BidiClientPayload
        {
            ClientContent = new()
            {
                Turns = content != null ? [content] : null,
                TurnComplete = true
            }
        });
    }

    public async Task CancelModelResponse()
    {

    }

    public async Task RemoveConversationItem(string itemId)
    {

    }

    private Task AttachEvents(MultiModalLiveClient client)
    {
        client.Connected += (sender, e) =>
        {
            _logger.LogInformation("Google Realtime Client connected.");
            _onModelReady();
        };

        client.Disconnected += (sender, e) =>
        {
            _logger.LogInformation("Google Realtime Client disconnected.");
        };

        client.MessageReceived += async (sender, e) =>
        {
            _logger.LogInformation("User message received.");
            if (e.Payload.SetupComplete != null)
            {
                _onConversationItemCreated(_client.ConnectionId.ToString());
            }

            if (e.Payload.ServerContent != null)
            {
                if (e.Payload.ServerContent.TurnComplete == true)
                {
                    var responseDone = await ResponseDone(_conn, e.Payload.ServerContent);
                    _onModelResponseDone(responseDone);
                }
            }
        };

        client.AudioChunkReceived += (sender, e) =>
        {
            _onModelAudioDeltaReceived(Convert.ToBase64String(e.Buffer), Guid.NewGuid().ToString());
        };

        client.TextChunkReceived += (sender, e) =>
        {
            _onInputAudioTranscriptionDone(new RoleDialogModel(AgentRole.Assistant, e.Text));
        };

        client.GenerationInterrupted += (sender, e) => 
        {
            _logger.LogInformation("Audio generation interrupted.");
            _onUserInterrupted(); 
        };

        client.AudioReceiveCompleted += (sender, e) => 
        {
            _logger.LogInformation("Audio receive completed.");
            _onModelAudioResponseDone(); 
        };

        client.ErrorOccurred += (sender, e) =>
        {
            var ex = e.GetException();
            _logger.LogError(ex, "Error occurred in Google Realtime Client");
        };

        return Task.CompletedTask;
    }

    private async Task<List<RoleDialogModel>> ResponseDone(RealtimeHubConnection conn,
        BidiGenerateContentServerContent serverContent)
    {
        var outputs = new List<RoleDialogModel>();

        var parts = serverContent.ModelTurn?.Parts;
        if (parts != null)
        {
            foreach (var part in parts)
            {
                var call = part.FunctionCall;
                if (call != null)
                {
                    var item = new RoleDialogModel(AgentRole.Assistant, part.Text)
                    {
                        CurrentAgentId = conn.CurrentAgentId,
                        MessageId = call.Id ?? String.Empty,
                        MessageType = MessageTypeName.FunctionCall
                    };
                    outputs.Add(item);
                }
                else
                {
                    var item = new RoleDialogModel(AgentRole.Assistant, call.Args?.ToJsonString() ?? string.Empty)
                    {
                        CurrentAgentId = conn.CurrentAgentId,
                        FunctionName = call.Name,
                        FunctionArgs = call.Args?.ToJsonString() ?? string.Empty,
                        ToolCallId = call.Id ?? String.Empty,
                        MessageId = call.Id ?? String.Empty,
                        MessageType = MessageTypeName.FunctionCall
                    };
                    outputs.Add(item);
                }
            }
        }

        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();
        // After chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, "response.done")
            {
                CurrentAgentId = conn.CurrentAgentId
            }, new TokenStatsModel
            {
                Provider = Provider,
                Model = _model,
            });
        }

        return outputs;
    }

    public async Task SendEventToModel(object message)
    {
        //todo Send Audio Chunks to Model, Botsharp RealTime Implementation seems to be incomplete

        if (_session == null) return;

        await _session.SendEventToModel(message);
    }

    public async Task<string> UpdateSession(RealtimeHubConnection conn, bool isInit = false)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var conv = await convService.GetConversation(conn.ConversationId);

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(conn.CurrentAgentId);

        var (prompt, request) = PrepareOptions(agent, []);

        var config = request.GenerationConfig;
        //Output Modality can either be text or audio
        if (config != null)
        {
            config.ResponseModalities = [Modality.AUDIO];

            var words = new List<string>();
            HookEmitter.Emit<IRealtimeHook>(_services, hook => words.AddRange(hook.OnModelTranscriptPrompt(agent)));

            var realtimeModelSettings = _services.GetRequiredService<RealtimeModelSettings>();

            config.Temperature = Math.Max(realtimeModelSettings.Temperature, 0.6f);
            config.MaxOutputTokens = realtimeModelSettings.MaxResponseOutputTokens;
        }
        

        var functions = request.Tools?.SelectMany(s => s.FunctionDeclarations).Select(x =>
        {
            var fn = new FunctionDef
            {
                Name = x.Name ?? string.Empty,
                Description = x.Description ?? string.Empty,
                Parameters = x.Parameters != null
                            ? JsonSerializer.Deserialize<FunctionParametersDef>(JsonSerializer.Serialize(x.Parameters))
                            : null
            };
            return fn;
        }).ToArray();

        await HookEmitter.Emit<IContentGeneratingHook>(_services,
            async hook => { await hook.OnSessionUpdated(agent, prompt, functions, isInit); });
        
        if (_settings.Gemini.UseGoogleSearch)
        {
            request.Tools ??= [];
            request.Tools.Add(new Tool()
            {
                GoogleSearch = new GoogleSearchTool()
            });
        }

        //await _client.SendSetupAsync(new BidiGenerateContentSetup()
        //{
        //    GenerationConfig = config,
        //    Model = Model.ToModelId(),
        //    SystemInstruction = request.SystemInstruction,
        //    //Tools = request.Tools?.ToArray(),
        //});

        await SendEventToModel(new BidiClientPayload
        {
            Setup = new BidiGenerateContentSetup()
            {
                GenerationConfig = config,
                Model = Model.ToModelId(),
                SystemInstruction = request.SystemInstruction,
                Tools = []
            }
        });

        return prompt;
    }

    public async Task InsertConversationItem(RoleDialogModel message)
    {
        //if (_client == null)
        //    throw new Exception("Client is not initialized");
        if (message.Role == AgentRole.Function)
        {
            var function = new FunctionResponse()
            {
                Name = message.FunctionName ?? string.Empty,
                Response = JsonNode.Parse(message.Content ?? "{}")
            };

            //await _client.SendToolResponseAsync(new BidiGenerateContentToolResponse()
            //{
            //    FunctionResponses = [function]
            //});

            await SendEventToModel(new BidiClientPayload
            {
                ToolResponse = new()
                {
                    FunctionResponses = [function]
                }
            });
        }
        else if (message.Role == AgentRole.Assistant)
        {
            await SendEventToModel(new BidiClientPayload
            {
                ClientContent = new()
                {
                    Turns = [new Content(message.Content, AgentRole.Model)],
                    TurnComplete = true
                }
            });
        }
        else if (message.Role == AgentRole.User)
        {
            //await _client.SentTextAsync(message.Content);

            await SendEventToModel(new BidiClientPayload
            {
                ClientContent = new()
                {
                    Turns = [new Content(message.Content, AgentRole.User)],
                    TurnComplete = true
                }
            });
        }
        else
        {
            throw new NotImplementedException("");
        }
    }

    public async Task<List<RoleDialogModel>> OnResponsedDone(RealtimeHubConnection conn, string response)
    {
        return [];
    }


    public async Task<RoleDialogModel> OnConversationItemCreated(RealtimeHubConnection conn, string response)
    {
        return await Task.FromResult(new RoleDialogModel(AgentRole.User, response));
    }

    private (string, GenerateContentRequest) PrepareOptions(Agent agent,
        List<RoleDialogModel> conversations)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var googleSettings = _settings;
        renderedInstructions = [];

        // Assembly messages
        var contents = new List<Content>();
        var tools = new List<Tool>();
        var funcDeclarations = new List<FunctionDeclaration>();

        var systemPrompts = new List<string>();
        if (!string.IsNullOrEmpty(agent.Instruction) || !agent.SecondaryInstructions.IsNullOrEmpty())
        {
            var instruction = agentService.RenderedInstruction(agent);
            renderedInstructions.Add(instruction);
            systemPrompts.Add(instruction);
        }

        var funcPrompts = new List<string>();
        var functions = agent.Functions.Concat(agent.SecondaryFunctions ?? []);
        foreach (var function in functions)
        {
            if (!agentService.RenderFunction(agent, function)) continue;

            var def = agentService.RenderFunctionProperty(agent, function);
            var props = JsonSerializer.Serialize(def?.Properties);
            var parameters = !string.IsNullOrWhiteSpace(props) && props != "{}"
                ? new Schema()
                {
                    Type = "object",
                    Properties = JsonSerializer.Deserialize<Dictionary<string, Schema>>(props),
                    Required = def?.Required ?? []
                }
                : null;

            funcDeclarations.Add(new FunctionDeclaration
            {
                Name = function.Name,
                Description = function.Description,
                Parameters = parameters
            });

            funcPrompts.Add($"{function.Name}: {function.Description} {def}");
        }

        if (!funcDeclarations.IsNullOrEmpty())
        {
            tools.Add(new Tool { FunctionDeclarations = funcDeclarations });
        }

        var convPrompts = new List<string>();
        foreach (var message in conversations)
        {
            if (message.Role == AgentRole.Function)
            {
                contents.Add(new Content([
                    new Part()
                    {
                        FunctionCall = new FunctionCall
                        {
                            Name = message.FunctionName,
                            Args = JsonNode.Parse(message.FunctionArgs ?? "{}")
                        }
                    }
                ], AgentRole.Model));

                contents.Add(new Content([
                    new Part()
                    {
                        FunctionResponse = new FunctionResponse
                        {
                            Name = message.FunctionName ?? string.Empty,
                            Response = new JsonObject()
                            {
                                ["result"] = message.Content ?? string.Empty
                            }
                        }
                    }
                ], AgentRole.Function));

                convPrompts.Add(
                    $"{AgentRole.Assistant}: Call function {message.FunctionName}({message.FunctionArgs}) => {message.Content}");
            }
            else if (message.Role == AgentRole.User)
            {
                var text = !string.IsNullOrWhiteSpace(message.Payload) ? message.Payload : message.Content;
                contents.Add(new Content(text, AgentRole.User));
                convPrompts.Add($"{AgentRole.User}: {text}");
            }
            else if (message.Role == AgentRole.Assistant)
            {
                contents.Add(new Content(message.Content, AgentRole.Model));
                convPrompts.Add($"{AgentRole.Assistant}: {message.Content}");
            }
        }

        var state = _services.GetRequiredService<IConversationStateService>();
        var temperature = float.Parse(state.GetState("temperature", "0.0"));
        var maxTokens = int.TryParse(state.GetState("max_tokens"), out var tokens)
            ? tokens
            : agent.LlmConfig?.MaxOutputTokens ?? LlmConstant.DEFAULT_MAX_OUTPUT_TOKEN;
        
        var request = new GenerateContentRequest
        {
            SystemInstruction = !systemPrompts.IsNullOrEmpty()
                ? new Content(systemPrompts[0], AgentRole.System)
                : null,
            Contents = contents,
            Tools = tools,
            GenerationConfig = new()
            {
                Temperature = temperature,
                MaxOutputTokens = maxTokens
            }
        };

        var prompt = GetPrompt(systemPrompts, funcPrompts, convPrompts);
        return (prompt, request);
    }

    private string GetPrompt(IEnumerable<string> systemPrompts, IEnumerable<string> funcPrompts,
        IEnumerable<string> convPrompts)
    {
        string prompt = string.Join("\r\n\r\n", systemPrompts);

        if (!funcPrompts.IsNullOrEmpty())
        {
            prompt += "\r\n\r\n[FUNCTIONS]\r\n";
            prompt += string.Join("\r\n", funcPrompts);
        }

        if (!convPrompts.IsNullOrEmpty())
        {
            prompt += "\r\n\r\n[CONVERSATION]\r\n";
            prompt += string.Join("\r\n", convPrompts);
        }

        return prompt;
    }
}