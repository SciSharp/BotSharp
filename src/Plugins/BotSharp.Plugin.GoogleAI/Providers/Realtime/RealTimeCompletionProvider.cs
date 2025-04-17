using GenerativeAI;
using GenerativeAI.Core;
using GenerativeAI.Live;
using GenerativeAI.Live.Extensions;
using GenerativeAI.Types;

namespace BotSharp.Plugin.GoogleAi.Providers.Realtime;

public class GoogleRealTimeProvider : IRealTimeCompletion
{
    public string Provider => "google-ai";
    private string _model = GoogleAIModels.Gemini2FlashExp;
    public string Model => _model;
    private MultiModalLiveClient _client;
    private GenerativeModel _chatClient;
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private List<string> renderedInstructions = [];

    private readonly GoogleAiSettings _settings;

    public GoogleRealTimeProvider(
        IServiceProvider services,
        GoogleAiSettings settings,
        ILogger<GoogleRealTimeProvider> logger)
    {
        _settings = settings;
        _services = services;
        _logger = logger;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    private Action onModelReady;
    Action<string, string> onModelAudioDeltaReceived;
    private Action onModelAudioResponseDone;
    Action<string> onModelAudioTranscriptDone;
    private Action<List<RoleDialogModel>> onModelResponseDone;
    Action<string> onConversationItemCreated;
    private Action<RoleDialogModel> onInputAudioTranscriptionCompleted;
    Action onUserInterrupted;
    RealtimeHubConnection conn;

    public async Task Connect(RealtimeHubConnection conn,
        Action onModelReady,
        Action<string, string> onModelAudioDeltaReceived,
        Action onModelAudioResponseDone,
        Action<string> onModelAudioTranscriptDone,
        Action<List<RoleDialogModel>> onModelResponseDone,
        Action<string> onConversationItemCreated,
        Action<RoleDialogModel> onInputAudioTranscriptionCompleted,
        Action onUserInterrupted)
    {
        this.conn = conn;
        this.onModelReady = onModelReady;
        this.onModelAudioDeltaReceived = onModelAudioDeltaReceived;
        this.onModelAudioResponseDone = onModelAudioResponseDone;
        this.onModelAudioTranscriptDone = onModelAudioTranscriptDone;
        this.onModelResponseDone = onModelResponseDone;
        this.onConversationItemCreated = onConversationItemCreated;
        this.onInputAudioTranscriptionCompleted = onInputAudioTranscriptionCompleted;
        this.onUserInterrupted = onUserInterrupted;

        var realtimeModelSettings = _services.GetRequiredService<RealtimeModelSettings>();
        _model = realtimeModelSettings.Model;

        var client = ProviderHelper.GetGeminiClient(Provider, _model, _services);
        _chatClient = client.CreateGenerativeModel(_model);
        _client = _chatClient.CreateMultiModalLiveClient(
            config: new GenerationConfig
            {
                ResponseModalities = [Modality.AUDIO],
            }, 
            systemInstruction: "You are a helpful assistant.",
            logger: _logger);

        await AttachEvents(_client);

        await _client.ConnectAsync(false);
    }

    public async Task Disconnect()
    {
        if (_client != null)
            await _client.DisconnectAsync();
    }

    public async Task AppenAudioBuffer(string message)
    {
        await _client.SendAudioAsync(Convert.FromBase64String(message));
    }

    public async Task AppenAudioBuffer(ArraySegment<byte> data, int length)
    {
        var buffer = data.AsSpan(0, length).ToArray();
        await _client.SendAudioAsync(buffer,"audio/pcm;rate=16000");
    }

    public async Task TriggerModelInference(string? instructions = null)
    {
        await _client.SendClientContentAsync(new BidiGenerateContentClientContent()
        {
            TurnComplete = true,
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
            onModelReady();
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
                onConversationItemCreated(_client.ConnectionId.ToString());
            }

            if (e.Payload.ServerContent != null)
            {
                if (e.Payload.ServerContent.TurnComplete == true)
                {
                    var responseDone = await ResponseDone(conn, e.Payload.ServerContent);
                    onModelResponseDone(responseDone);
                }
            }
        };

        client.AudioChunkReceived += (sender, e) =>
        {
            onModelAudioDeltaReceived(Convert.ToBase64String(e.Buffer), Guid.NewGuid().ToString());
        };

        client.TextChunkReceived += (sender, e) =>
        {
            onInputAudioTranscriptionCompleted(new RoleDialogModel(AgentRole.Assistant, e.Text));
        };

        client.GenerationInterrupted += (sender, e) => 
        {
            _logger.LogInformation("Audio generation interrupted.");
            onUserInterrupted(); 
        };

        client.AudioReceiveCompleted += (sender, e) => 
        {
            _logger.LogInformation("Audio receive completed.");
            onModelAudioResponseDone(); 
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
    }

    public async Task<string> UpdateSession(RealtimeHubConnection conn)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var conv = await convService.GetConversation(conn.ConversationId);

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(conn.CurrentAgentId);

        var (prompt, request) = PrepareOptions(_chatClient, agent, new List<RoleDialogModel>());

        var config = request.GenerationConfig;
        //Output Modality can either be text or audio
        if (config != null)
        {
            config.ResponseModalities = new List<Modality>([Modality.AUDIO]);

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
            };
            fn.Parameters = x.Parameters != null
                ? JsonSerializer.Deserialize<FunctionParametersDef>(JsonSerializer.Serialize(x.Parameters))
                : null;
            return fn;
        }).ToArray();

        await HookEmitter.Emit<IContentGeneratingHook>(_services,
            async hook => { await hook.OnSessionUpdated(agent, prompt, functions); });
        
        if (_settings.Gemini.UseGoogleSearch)
        {
            if (request.Tools == null)
                request.Tools = new List<Tool>();
            request.Tools.Add(new Tool()
            {
                GoogleSearch = new GoogleSearchTool()
            });
        }

        // if(request.Tools.Count == 0)
        //     request.Tools = null;
        // config.MaxOutputTokens = null;
        
        await _client.SendSetupAsync(new BidiGenerateContentSetup()
        {
            GenerationConfig = config,
            Model = Model.ToModelId(),
            SystemInstruction = request.SystemInstruction,
            Tools = request.Tools?.ToArray(),
        });

        return prompt;
    }

    public async Task InsertConversationItem(RoleDialogModel message)
    {
        if (_client == null)
            throw new Exception("Client is not initialized");
        if (message.Role == AgentRole.Function)
        {
            var function = new FunctionResponse()
            {
                Name = message.FunctionName ?? string.Empty,
                Response = JsonNode.Parse(message.Content ?? "{}")
            };

            await _client.SendToolResponseAsync(new BidiGenerateContentToolResponse()
            {
                FunctionResponses = [function]
            });
        }
        else if (message.Role == AgentRole.Assistant)
        {
        }
        else if (message.Role == AgentRole.User)
        {
            await _client.SentTextAsync(message.Content);
        }
        else
        {
            throw new NotImplementedException("");
        }
    }

    public Task<List<RoleDialogModel>> OnResponsedDone(RealtimeHubConnection conn, string response)
    {
        throw new NotImplementedException("");
    }


    public Task<RoleDialogModel> OnConversationItemCreated(RealtimeHubConnection conn, string response)
    {
        return Task.FromResult(new RoleDialogModel(AgentRole.User, response));
    }

    private (string, GenerateContentRequest) PrepareOptions(GenerativeModel aiModel, Agent agent,
        List<RoleDialogModel> conversations)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var googleSettings = _settings;
        renderedInstructions = [];

        // Add settings
        aiModel.UseGoogleSearch = googleSettings.Gemini.UseGoogleSearch;
        aiModel.UseGrounding = googleSettings.Gemini.UseGrounding;

        aiModel.FunctionCallingBehaviour = new FunctionCallingBehaviour()
        {
            AutoCallFunction = false
        };

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