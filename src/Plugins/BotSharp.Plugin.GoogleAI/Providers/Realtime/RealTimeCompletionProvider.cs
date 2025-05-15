using System.Threading;
using BotSharp.Abstraction.Realtime.Models.Session;
using BotSharp.Core.Session;
using BotSharp.Plugin.GoogleAI.Models.Realtime;
using GenerativeAI;
using GenerativeAI.Types;
using GenerativeAI.Types.Converters;

namespace BotSharp.Plugin.GoogleAi.Providers.Realtime;

public class GoogleRealTimeProvider : IRealTimeCompletion
{
    public string Provider => "google-ai";
    public string Model => _model;

    private string _model = GoogleAIModels.Gemini2FlashExp;

    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private List<string> renderedInstructions = [];

    private LlmRealtimeSession _session;
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

        var uri = BuildWebsocketUri(modelSettings.ApiKey, "v1beta");
        await _session.ConnectAsync(uri: uri, cancellationToken: CancellationToken.None);

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
    }

    private async Task ReceiveMessage(
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
        using var inputStream = new RealtimeTranscriptionResponse();
        using var outputStream = new RealtimeTranscriptionResponse();

        await foreach (ChatSessionUpdate update in _session.ReceiveUpdatesAsync(CancellationToken.None))
        {
            var receivedText = update?.RawResponse;
            if (string.IsNullOrEmpty(receivedText))
            {
                continue;
            }

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
                else if (response.SessionResumptionUpdate != null)
                {
                    _logger.LogInformation($"Session resumption update => New handle: {response.SessionResumptionUpdate.NewHandle}, Resumable: {response.SessionResumptionUpdate.Resumable}");
                }
                else if (response.ToolCall != null && !response.ToolCall.FunctionCalls.IsNullOrEmpty())
                {
                    var functionCall = response.ToolCall.FunctionCalls!.First();

                    _logger.LogInformation($"Tool call received: {functionCall.Name}({functionCall.Args?.ToJsonString(_jsonOptions) ?? string.Empty}).");

                    if (functionCall != null)
                    {
                        var messages = OnFunctionCall(conn, functionCall);
                        await onModelResponseDone(messages);
                    }
                }
                else if (response.ServerContent != null)
                {
                    if (response.ServerContent.InputTranscription?.Text != null)
                    {
                        inputStream.Collect(response.ServerContent.InputTranscription.Text);
                    }

                    if (response.ServerContent.OutputTranscription?.Text != null)
                    {
                        outputStream.Collect(response.ServerContent.OutputTranscription.Text);
                    }

                    if (response.ServerContent.ModelTurn != null)
                    {
                        _logger.LogInformation($"Model audio delta received.");

                        // Handle input transcription
                        var inputTranscription = inputStream.GetText();
                        if (!string.IsNullOrEmpty(inputTranscription))
                        {
                            var message = OnUserAudioTranscriptionCompleted(conn, inputTranscription);
                            await onInputAudioTranscriptionDone(message);
                        }
                        inputStream.Clear();

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

                        // Handle output transcription
                        var outputTranscription = outputStream.GetText();
                        if (!string.IsNullOrEmpty(outputTranscription))
                        {
                            var messages = await OnResponseDone(conn, outputTranscription, response.UsageMetaData);
                            await onModelResponseDone(messages);
                        }
                        inputStream.Clear();
                        outputStream.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error when deserializing server response. {ex.Message}");
                break;
            }
        }

        _session.Dispose();
    }


    public async Task Disconnect()
    {
        if (_session != null)
        {
            await _session.DisconnectAsync();
        }
    }

    public async Task AppenAudioBuffer(string message)
    {
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
        var content = new Content(instructions ?? "Please respond to user.", AgentRole.User);

        await SendEventToModel(new BidiClientPayload
        {
            ClientContent = new()
            {
                Turns = [content],
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

    public async Task SendEventToModel(object message)
    {
        if (_session == null) return;

        await _session.SendEventToModelAsync(message);
    }

    public async Task<string> UpdateSession(RealtimeHubConnection conn, bool isInit = false)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var realtimeSetting = _services.GetRequiredService<RealtimeModelSettings>();

        var agent = await agentService.LoadAgent(conn.CurrentAgentId);
        var (prompt, request) = PrepareOptions(agent, []);

        var config = request.GenerationConfig;
        if (config != null)
        {
            //Output Modality can either be text or audio
            config.ResponseModalities = [Modality.AUDIO];

            var words = new List<string>();
            HookEmitter.Emit<IRealtimeHook>(_services, hook => words.AddRange(hook.OnModelTranscriptPrompt(agent)));

            config.Temperature = Math.Max(realtimeSetting.Temperature, 0.6f);
            config.MaxOutputTokens = realtimeSetting.MaxResponseOutputTokens;
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

        await SendEventToModel(new RealtimeClientPayload
        {
            Setup = new RealtimeGenerateContentSetup()
            {
                GenerationConfig = config,
                Model = Model.ToModelId(),
                SystemInstruction = request.SystemInstruction,
                Tools = request.Tools?.ToArray(),
                InputAudioTranscription = realtimeSetting.InputAudioTranscribe ? new() : null,
                OutputAudioTranscription = realtimeSetting.InputAudioTranscribe ? new() : null,
                SessionResumption = new()
            }
        });

        return prompt;
    }

    public async Task InsertConversationItem(RoleDialogModel message)
    {
        if (message.Role == AgentRole.Function)
        {
            var function = new FunctionResponse()
            {
                Id = message.ToolCallId,
                Name = message.FunctionName ?? string.Empty,
                Response = new JsonObject()
                {
                    ["result"] = message.Content ?? string.Empty
                }
            };

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
            throw new NotImplementedException($"Unrecognized role {message.Role}.");
        }
    }

    #region Private methods
    private List<RoleDialogModel> OnFunctionCall(RealtimeHubConnection conn, RealtimeFunctionCall functionCall)
    {
        var outputs = new List<RoleDialogModel>
        {
            new(AgentRole.Assistant, string.Empty)
            {
                CurrentAgentId = conn.CurrentAgentId,
                FunctionName = functionCall.Name,
                FunctionArgs = functionCall.Args?.ToJsonString(_jsonOptions),
                ToolCallId = functionCall.Id,
                MessageType = MessageTypeName.FunctionCall
            }
        };

        return outputs;
    }


    private async Task<List<RoleDialogModel>> OnResponseDone(RealtimeHubConnection conn, string text, RealtimeUsageMetaData? usage)
    {
        var outputs = new List<RoleDialogModel>
        {
            new(AgentRole.Assistant, text)
            {
                CurrentAgentId = conn.CurrentAgentId,
                MessageId = Guid.NewGuid().ToString(),
                MessageType = MessageTypeName.Plain
            }
        };

        if (usage != null)
        {
            var contentHooks = _services.GetServices<IContentGeneratingHook>();
            foreach (var hook in contentHooks)
            {
                await hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, text)
                {
                    CurrentAgentId = conn.CurrentAgentId
                },
                new TokenStatsModel
                {
                    Provider = Provider,
                    Model = _model,
                    Prompt = text,
                    TextInputTokens = usage.PromptTokensDetails?.FirstOrDefault(x => x.Modality == Modality.TEXT.ToString())?.TokenCount ?? 0,
                    AudioInputTokens = usage.PromptTokensDetails?.FirstOrDefault(x => x.Modality == Modality.AUDIO.ToString())?.TokenCount ?? 0,
                    TextOutputTokens = usage.ResponseTokensDetails?.FirstOrDefault(x => x.Modality == Modality.TEXT.ToString())?.TokenCount ?? 0,
                    AudioOutputTokens = usage.ResponseTokensDetails?.FirstOrDefault(x => x.Modality == Modality.AUDIO.ToString())?.TokenCount ?? 0
                });
            }
        }

        return outputs;
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


    private RoleDialogModel OnUserAudioTranscriptionCompleted(RealtimeHubConnection conn, string text)
    {
        return new RoleDialogModel(AgentRole.User, text)
        {
            CurrentAgentId = conn.CurrentAgentId
        };
    }

    private Uri BuildWebsocketUri(string apiKey, string version = "v1alpha")
    {
        return new Uri($"wss://generativelanguage.googleapis.com/ws/google.ai.generativelanguage.{version}.GenerativeService.BidiGenerateContent?key={apiKey}");
    }
    #endregion
}