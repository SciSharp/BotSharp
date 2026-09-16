using BotSharp.Abstraction.Realtime.Settings;
using OpenAI.Chat;

namespace BotSharp.Plugin.OpenAI.Providers.Live;

public partial class LiveCompletionProvider
{
    /// <summary>
    /// Pushes the agent's current instruction and tools to the backend handler.
    /// Only delegation.responses may change once a session is running, so the rest of the
    /// session object is sent once with session.start and left alone afterwards.
    /// </summary>
    public async Task<string> UpdateSession(RealtimeHubConnection conn, bool isInit = false)
    {
        var (sessionConfig, instruction) = await BuildSessionConfig(conn, isInit);

        // session.start already carried this configuration.
        if (isInit)
        {
            return instruction;
        }

        if (sessionConfig.Delegation?.Responses != null)
        {
            await SendEventToModel(new
            {
                type = LiveClientEventType.SessionUpdate,
                event_id = NewEventId("update"),
                session = new
                {
                    delegation = new
                    {
                        type = sessionConfig.Delegation.Type,
                        responses = sessionConfig.Delegation.Responses
                    }
                }
            });
        }

        // The agent instruction reaches the backend through delegation.responses.instructions
        // above, so nothing is appended to the voice model here.
        await Task.Delay(300);
        return instruction;
    }

    #region Session config
    private async Task<(LiveSessionConfig, string)> BuildSessionConfig(RealtimeHubConnection conn, bool isInit)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var realtimeModelSettings = _services.GetRequiredService<RealtimeModelSettings>();
        var liveSettings = LiveSettings;

        var agent = await agentService.LoadAgent(conn.CurrentAgentId);
        var (_, messages, options) = PrepareOptions(agent, []);

        var instruction = messages.FirstOrDefault()?.Content.FirstOrDefault()?.Text ?? agent?.Description ?? string.Empty;
        var functions = options.Tools.Select(x => new FunctionDef
        {
            Name = x.FunctionName,
            Description = x.FunctionDescription,
            Parameters = JsonSerializer.Deserialize<FunctionParametersDef>(x.FunctionParameters)
        }).ToArray();

        _lastAgentInstruction = instruction;

        var config = new LiveSessionConfig
        {
            Model = _model,
            // The voice model gets conversation style and a delegation policy; the agent
            // instruction is workflow detail and belongs to the backend handler instead.
            Instructions = liveSettings.VoiceInstructions.IfNullOrEmptyAs(LivePromptConstants.DefaultVoiceInstruction),
            Store = liveSettings.Store ? true : null,
            Audio = BuildAudioConfig(realtimeModelSettings, liveSettings),
            Delegation = BuildDelegationConfig(agent, instruction, functions, realtimeModelSettings, liveSettings)
        };

        await HookEmitter.Emit<IContentGeneratingHook>(_services, async hook =>
        {
            await hook.OnSessionUpdated(agent, instruction, functions, isInit);
        }, agent.Id);

        return (config, instruction);
    }

    private LiveAudioConfig BuildAudioConfig(RealtimeModelSettings realtimeModelSettings, LiveSettings liveSettings)
    {
        // A Live WebSocket carries a single negotiated format in both directions, so the
        // output format wins when the two realtime settings disagree.
        var format = _realtimeOptions?.OutputAudioFormat
            ?? _realtimeOptions?.InputAudioFormat
            ?? realtimeModelSettings.OutputAudioFormat
            ?? realtimeModelSettings.InputAudioFormat;

        // Live accepts only "format" and "output" here. It has no audio.input section:
        // noise reduction and turn detection are handled inside the model.
        return new LiveAudioConfig
        {
            Format = ConvertAudioFormat(format),
            Output = new LiveOutputAudioConfig
            {
                Voice = liveSettings.Voice
            }
        };
    }

    private LiveDelegationConfig BuildDelegationConfig(
        Agent? agent,
        string instruction,
        FunctionDef[] functions,
        RealtimeModelSettings realtimeModelSettings,
        LiveSettings liveSettings)
    {
        if (liveSettings.DelegationType == LiveDelegationType.Client)
        {
            // The application owns routing, context and tool execution.
            return new LiveDelegationConfig { Type = LiveDelegationType.Client };
        }

        var reasoningEffort = GetReasoningEffort(agent);

        return new LiveDelegationConfig
        {
            Type = LiveDelegationType.Responses,
            Responses = new LiveResponsesDelegationConfig
            {
                Model = liveSettings.BackendModel,
                Instructions = instruction,
                Tools = functions,
                ToolChoice = "auto",
                ParallelToolCalls = liveSettings.ParallelToolCalls,
                MaxOutputTokens = realtimeModelSettings.MaxResponseOutputTokens >= 16
                    ? realtimeModelSettings.MaxResponseOutputTokens
                    : null,
                ServiceTier = liveSettings.ServiceTier,
                Reasoning = !string.IsNullOrWhiteSpace(reasoningEffort)
                    ? new LiveReasoningConfig { Effort = reasoningEffort }
                    : null
            }
        };
    }

    /// <summary>
    /// Maps the BotSharp audio format names onto the media types Live accepts:
    /// audio/pcm at 24k or 16k, audio/pcmu and audio/pcma at 8k.
    /// </summary>
    private LiveAudioFormat ConvertAudioFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return new LiveAudioFormat { Type = "audio/pcm", Rate = 24000 };
        }

        switch (format.ToLowerInvariant())
        {
            case "pcm16":
            case "audio/pcm":
                return new LiveAudioFormat { Type = "audio/pcm", Rate = 24000 };
            case "pcm16_16k":
                return new LiveAudioFormat { Type = "audio/pcm", Rate = 16000 };
            case "g711_ulaw":
            case "audio/pcmu":
                return new LiveAudioFormat { Type = "audio/pcmu", Rate = 8000 };
            case "g711_alaw":
            case "audio/pcma":
                return new LiveAudioFormat { Type = "audio/pcma", Rate = 8000 };
            default:
                _logger.LogWarning("Unknown audio format {Format} for {Provider}, falling back to audio/pcm 24kHz.", format, Provider);
                return new LiveAudioFormat { Type = "audio/pcm", Rate = 24000 };
        }
    }

    private string? GetReasoningEffort(Agent? agent)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var reasoningEffort = state.GetState("reasoning_effort_level");

        if (string.IsNullOrEmpty(reasoningEffort) && _model == agent?.LlmConfig?.Realtime?.Model)
        {
            reasoningEffort = agent?.LlmConfig?.Realtime?.ReasoningEffortLevel;
        }

        if (string.IsNullOrEmpty(reasoningEffort))
        {
            var settings = GetModelSetting()?.Reasoning;

            reasoningEffort = settings?.EffortLevel;
            if (settings?.Parameters != null
                && settings.Parameters.TryGetValue("EffortLevel", out var settingValue)
                && !string.IsNullOrEmpty(settingValue?.Default))
            {
                reasoningEffort = settingValue.Default;
            }
        }

        return reasoningEffort;
    }
    #endregion

    #region Instruction and tool preparation
    private (string, IEnumerable<ChatMessage>, ChatCompletionOptions) PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();

        var messages = new List<ChatMessage>();

        var temperature = float.Parse(state.GetState("temperature", "0.0"));
        var maxTokens = int.TryParse(state.GetState("max_tokens"), out var tokens)
                            ? tokens
                            : agent.LlmConfig?.MaxOutputTokens ?? LlmConstant.DEFAULT_MAX_OUTPUT_TOKEN;
        var options = new ChatCompletionOptions()
        {
            ToolChoice = ChatToolChoice.CreateAutoChoice(),
            Temperature = temperature,
            MaxOutputTokenCount = maxTokens
        };

        // Prepare instruction and functions
        var renderData = agentService.CollectRenderData(agent);
        var (instruction, functions) = agentService.PrepareInstructionAndFunctions(agent, renderData);
        if (!string.IsNullOrWhiteSpace(instruction))
        {
            messages.Add(new SystemChatMessage(instruction));
        }

        foreach (var function in functions)
        {
            if (!agentService.RenderFunction(agent, function, renderData))
            {
                continue;
            }

            var property = agentService.RenderFunctionProperty(agent, function, renderData);

            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: function.Name,
                functionDescription: function.Description,
                functionParameters: BinaryData.FromObjectAsJson(property)));
        }

        if (!string.IsNullOrEmpty(agent.Knowledges))
        {
            messages.Add(new SystemChatMessage(agent.Knowledges));
        }

        var samples = ProviderHelper.GetChatSamples(agent.Samples);
        foreach (var sample in samples)
        {
            messages.Add(sample.Role == AgentRole.User ? new UserChatMessage(sample.Content) : new AssistantChatMessage(sample.Content));
        }

        var filteredMessages = conversations.Select(x => x).ToList();
        var firstUserMsgIdx = filteredMessages.FindIndex(x => x.Role == AgentRole.User);
        if (firstUserMsgIdx > 0)
        {
            filteredMessages = filteredMessages.Where((_, idx) => idx >= firstUserMsgIdx).ToList();
        }

        foreach (var message in filteredMessages)
        {
            if (message.Role == AgentRole.Function)
            {
                messages.Add(new AssistantChatMessage(new List<ChatToolCall>
                {
                    ChatToolCall.CreateFunctionToolCall(message.ToolCallId.IfNullOrEmptyAs(message.FunctionName), message.FunctionName, BinaryData.FromString(message.FunctionArgs ?? "{}"))
                }));

                messages.Add(new ToolChatMessage(message.ToolCallId.IfNullOrEmptyAs(message.FunctionName), message.LlmContent));
            }
            else if (message.Role == AgentRole.User)
            {
                messages.Add(new UserChatMessage(message.LlmContent));
            }
            else if (message.Role == AgentRole.Assistant)
            {
                messages.Add(new AssistantChatMessage(message.LlmContent));
            }
        }

        return (instruction, messages, options);
    }
    #endregion
}
