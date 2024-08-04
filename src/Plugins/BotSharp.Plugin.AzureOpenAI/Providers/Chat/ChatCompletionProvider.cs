using OpenAI.Chat;

namespace BotSharp.Plugin.AzureOpenAI.Providers.Chat;

public class ChatCompletionProvider : IChatCompletion
{
    protected readonly AzureOpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger<ChatCompletionProvider> _logger;

    protected string _model;

    public virtual string Provider => "azure-openai";

    public ChatCompletionProvider(
        AzureOpenAiSettings settings,
        ILogger<ChatCompletionProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _logger = logger;
        _services = services;
    }

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        var response = chatClient.CompleteChat(messages, options);
        var value = response.Value;
        var reason = value.FinishReason;
        var content = value.Content;
        var text = content.FirstOrDefault()?.Text ?? string.Empty;

        RoleDialogModel responseMessage;
        if (reason == ChatFinishReason.FunctionCall)
        {
            responseMessage = new RoleDialogModel(AgentRole.Function, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.Last().MessageId,
                FunctionName = value.FunctionCall.FunctionName,
                FunctionArgs = value.FunctionCall.FunctionArguments
            };

            // Somethings LLM will generate a function name with agent name.
            if (!string.IsNullOrEmpty(responseMessage.FunctionName))
            {
                responseMessage.FunctionName = responseMessage.FunctionName.Split('.').Last();
            }
        }
        else if (reason == ChatFinishReason.ToolCalls)
        {
            var toolCall = value.ToolCalls.FirstOrDefault();
            responseMessage = new RoleDialogModel(AgentRole.Function, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.Last().MessageId,
                FunctionName = toolCall?.FunctionName,
                FunctionArgs = toolCall?.FunctionArguments
            };
        }
        else
        {
            responseMessage = new RoleDialogModel(AgentRole.Assistant, text)
            {
                CurrentAgentId = agent.Id,
                MessageId = conversations.Last().MessageId
            };
        }

        // After chat completion hook
        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                PromptCount = response.Value.Usage.InputTokens,
                CompletionCount = response.Value.Usage.OutputTokens
            });
        }

        return responseMessage;
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent,
        List<RoleDialogModel> conversations,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        foreach (var hook in hooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        var response = await chatClient.CompleteChatAsync(messages, options);
        var value = response.Value;
        var reason = value.FinishReason;
        var content = value.Content;
        var text = content.FirstOrDefault()?.Text ?? string.Empty;

        var msg = new RoleDialogModel(AgentRole.Assistant, text)
        {
            CurrentAgentId = agent.Id
        };

        // After chat completion hook
        foreach (var hook in hooks)
        {
            await hook.AfterGenerated(msg, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model,
                PromptCount = response.Value.Usage.InputTokens,
                CompletionCount = response.Value.Usage.OutputTokens
            });
        }

        if (reason == ChatFinishReason.FunctionCall)
        {
            _logger.LogInformation($"[{agent.Name}]: {value.FunctionCall.FunctionName}({value.FunctionCall.FunctionArguments})");

            var funcContextIn = new RoleDialogModel(AgentRole.Function, text)
            {
                CurrentAgentId = agent.Id,
                FunctionName = value.FunctionCall?.FunctionName,
                FunctionArgs = value.FunctionCall?.FunctionArguments
            };

            // Somethings LLM will generate a function name with agent name.
            if (!string.IsNullOrEmpty(funcContextIn.FunctionName))
            {
                funcContextIn.FunctionName = funcContextIn.FunctionName.Split('.').Last();
            }

            // Execute functions
            await onFunctionExecuting(funcContextIn);
        }
        else
        {
            // Text response received
            await onMessageReceived(msg);
        }

        return true;
    }

    public async Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
    {
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        var response = chatClient.CompleteChatStreamingAsync(messages, options);

        await foreach (var choice in response)
        {
            if (choice.FinishReason == ChatFinishReason.FunctionCall)
            {
                Console.Write(choice.FunctionCallUpdate?.FunctionArgumentsUpdate);

                await onMessageReceived(new RoleDialogModel(AgentRole.Assistant, choice.FunctionCallUpdate?.FunctionArgumentsUpdate));
                continue;
            }

            if (choice.ContentUpdate.IsNullOrEmpty()) continue;

            _logger.LogInformation(choice.ContentUpdate[0]?.Text);

            await onMessageReceived(new RoleDialogModel(choice.Role.ToString(), choice.ContentUpdate[0]?.Text ?? string.Empty));
        }

        return true;
    }

    protected (string, IEnumerable<ChatMessage>, ChatCompletionOptions) PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var fileService = _services.GetRequiredService<IBotSharpFileService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);
        var allowMultiModal = settings != null && settings.MultiModal;

        var messages = new List<ChatMessage>();

        var temperature = float.Parse(state.GetState("temperature", "0.0"));
        var maxTokens = int.Parse(state.GetState("max_tokens", "1024"));
        var options = new ChatCompletionOptions()
        {
            Temperature = temperature,
            MaxTokens = maxTokens
        };

        foreach (var function in agent.Functions)
        {
            if (!agentService.RenderFunction(agent, function)) continue;

            var property = agentService.RenderFunctionProperty(agent, function);

            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: function.Name,
                functionDescription: function.Description,
                functionParameters: BinaryData.FromObjectAsJson(property)));
        }

        if (!string.IsNullOrEmpty(agent.Instruction))
        {
            var instruction = agentService.RenderedInstruction(agent);
            messages.Add(new SystemChatMessage(instruction));
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

        foreach (var message in conversations)
        {
            if (message.Role == AgentRole.Function)
            {
                messages.Add(new AssistantChatMessage(string.Empty)
                {
                    FunctionCall = new ChatFunctionCall(message.FunctionName, message.FunctionArgs ?? string.Empty)
                });

                messages.Add(new FunctionChatMessage(message.FunctionName, message.Content));
            }
            else if (message.Role == AgentRole.User)
            {
                var text = !string.IsNullOrWhiteSpace(message.Payload) ? message.Payload : message.Content;
                var textPart = ChatMessageContentPart.CreateTextMessageContentPart(text);
                var contentParts = new List<ChatMessageContentPart> { textPart };

                if (allowMultiModal && !message.Files.IsNullOrEmpty())
                {
                    foreach (var file in message.Files)
                    {
                        if (!string.IsNullOrEmpty(file.FileUrl))
                        {
                            var uri = new Uri(file.FileUrl);
                            var contentPart = ChatMessageContentPart.CreateImageMessageContentPart(uri, ImageChatMessageContentPartDetail.Low);
                            contentParts.Add(contentPart);
                        }
                        else if (!string.IsNullOrEmpty(file.FileData))
                        {
                            var (contentType, bytes) = fileService.GetFileInfoFromData(file.FileData);
                            var contentPart = ChatMessageContentPart.CreateImageMessageContentPart(BinaryData.FromBytes(bytes), contentType, ImageChatMessageContentPartDetail.Low);
                            contentParts.Add(contentPart);
                        }
                        else if (!string.IsNullOrEmpty(file.FileStorageUrl))
                        {
                            var contentType = fileService.GetFileContentType(file.FileStorageUrl);
                            using var stream = File.OpenRead(file.FileStorageUrl);
                            var contentPart = ChatMessageContentPart.CreateImageMessageContentPart(BinaryData.FromStream(stream), contentType, ImageChatMessageContentPartDetail.Low);
                            contentParts.Add(contentPart);
                        }
                    }
                }
                messages.Add(new UserChatMessage(contentParts) { ParticipantName = message.FunctionName });
            }
            else if (message.Role == AgentRole.Assistant)
            {
                messages.Add(new AssistantChatMessage(message.Content));
            }
        }

        var prompt = GetPrompt(messages, options);
        return (prompt, messages, options);
    }

    private string GetPrompt(IEnumerable<ChatMessage> messages, ChatCompletionOptions options)
    {
        var prompt = string.Empty;

        if (!messages.IsNullOrEmpty())
        {
            // System instruction
            var verbose = string.Join("\r\n", messages
                .Select(x => x as SystemChatMessage)
                .Where(x => x != null)
                .Select(x =>
                {
                    if (!string.IsNullOrEmpty(x.ParticipantName))
                    {
                        // To display Agent name in log
                        return $"[{x.ParticipantName}]: {x.Content.FirstOrDefault()?.Text ?? string.Empty}";
                    }
                    return $"{AgentRole.System}: {x.Content.FirstOrDefault()?.Text ?? string.Empty}";
                }));
            prompt += $"{verbose}\r\n";

            prompt += "\r\n[CONVERSATION]";
            verbose = string.Join("\r\n", messages
                .Where(x => x as SystemChatMessage == null)
                .Select(x =>
                {
                    var fnMessage = x as FunctionChatMessage;
                    if (fnMessage != null)
                    {
                        return $"{AgentRole.Function}: {fnMessage.Content.FirstOrDefault()?.Text ?? string.Empty}";
                    }

                    var userMessage = x as UserChatMessage;
                    if (userMessage != null)
                    {
                        var content = x.Content.FirstOrDefault()?.Text ?? string.Empty;
                        return !string.IsNullOrEmpty(userMessage.ParticipantName) && userMessage.ParticipantName != "route_to_agent" ?
                            $"{userMessage.ParticipantName}: {content}" :
                            $"{AgentRole.User}: {content}";
                    }

                    var assistMessage = x as AssistantChatMessage;
                    if (assistMessage != null)
                    {
                        return assistMessage.FunctionCall != null ?
                            $"{AgentRole.Assistant}: Call function {assistMessage.FunctionCall.FunctionName}({assistMessage.FunctionCall.FunctionArguments})" :
                            $"{AgentRole.Assistant}: {assistMessage.Content.FirstOrDefault()?.Text ?? string.Empty}";
                    }

                    return string.Empty;
                }));
            prompt += $"\r\n{verbose}\r\n";
        }

        if (!options.Tools.IsNullOrEmpty())
        {
            var functions = string.Join("\r\n", options.Tools.Select(fn =>
            {
                return $"\r\n{fn.FunctionName}: {fn.FunctionDescription}\r\n{fn.FunctionParameters}";
            }));
            prompt += $"\r\n[FUNCTIONS]{functions}\r\n";
        }

        return prompt;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
