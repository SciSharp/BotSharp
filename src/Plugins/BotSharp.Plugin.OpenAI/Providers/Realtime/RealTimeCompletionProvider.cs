using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Functions.Models;
using BotSharp.Abstraction.Realtime.Models;
using OpenAI.Chat;
using System.Text.Json;

namespace BotSharp.Plugin.OpenAI.Providers.Realtime;

public class RealTimeCompletionProvider : IRealTimeCompletion
{
    public string Provider => "openai";

    protected readonly OpenAiSettings _settings;
    protected readonly IServiceProvider _services;
    protected readonly ILogger<RealTimeCompletionProvider> _logger;

    protected string _model = "gpt-4o-mini-realtime-preview-2024-12-17";

    public RealTimeCompletionProvider(
        OpenAiSettings settings,
        ILogger<RealTimeCompletionProvider> logger,
        IServiceProvider services)
    {
        _settings = settings;
        _logger = logger;
        _services = services;
    }

    public async Task<RealtimeSession> CreateSession(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();

        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var chatClient = client.GetChatClient(_model);
        var (prompt, messages, options) = PrepareOptions(agent, conversations);

        var args = new RealtimeSessionRequest
        {
            Instructions = prompt,
            ToolChoice = "auto",
            Tools = options.Tools.Select(x =>
            {
                var fn = new FunctionDef
                {
                    Name = x.FunctionName,
                    Description = x.FunctionDescription
                };
                fn.Parameters = JsonSerializer.Deserialize<FunctionParametersDef>(x.FunctionParameters);
                return fn;
            }).ToArray(),
        };

        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, args.Model);

        var api = _services.GetRequiredService<IOpenAiRealtimeApi>();
        var session = await api.GetSessionAsync(args, settings.ApiKey);
        return session;
    }

    protected (string, IEnumerable<ChatMessage>, ChatCompletionOptions) PrepareOptions(Agent agent, List<RoleDialogModel> conversations)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var fileStorage = _services.GetRequiredService<IFileStorageService>();
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);
        var allowMultiModal = settings != null && settings.MultiModal;

        var messages = new List<ChatMessage>();

        var temperature = float.Parse(state.GetState("temperature", "0.0"));
        var maxTokens = int.Parse(state.GetState("max_tokens", "1024"));
        var options = new ChatCompletionOptions()
        {
            ToolChoice = ChatToolChoice.CreateAutoChoice(),
            Temperature = temperature,
            MaxOutputTokenCount = maxTokens
        };

        var functions = agent.Functions.Concat(agent.SecondaryFunctions ?? []);
        foreach (var function in functions)
        {
            if (!agentService.RenderFunction(agent, function)) continue;

            var property = agentService.RenderFunctionProperty(agent, function);

            options.Tools.Add(ChatTool.CreateFunctionTool(
                functionName: function.Name,
                functionDescription: function.Description,
                functionParameters: BinaryData.FromObjectAsJson(property)));
        }

        if (!string.IsNullOrEmpty(agent.Instruction) || !agent.SecondaryInstructions.IsNullOrEmpty())
        {
            var text = agentService.RenderedInstruction(agent);
            messages.Add(new SystemChatMessage(text));
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
                    ChatToolCall.CreateFunctionToolCall(message.ToolCallId, message.FunctionName, BinaryData.FromString(message.FunctionArgs ?? string.Empty))
                }));

                messages.Add(new ToolChatMessage(message.ToolCallId, message.Content));
            }
            else if (message.Role == AgentRole.User)
            {
                var text = !string.IsNullOrWhiteSpace(message.Payload) ? message.Payload : message.Content;
                var textPart = ChatMessageContentPart.CreateTextPart(text);
                var contentParts = new List<ChatMessageContentPart> { textPart };

                if (allowMultiModal && !message.Files.IsNullOrEmpty())
                {
                    foreach (var file in message.Files)
                    {
                        if (!string.IsNullOrEmpty(file.FileData))
                        {
                            var (contentType, bytes) = FileUtility.GetFileInfoFromData(file.FileData);
                            var contentPart = ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), contentType, ChatImageDetailLevel.Auto);
                            contentParts.Add(contentPart);
                        }
                        else if (!string.IsNullOrEmpty(file.FileStorageUrl))
                        {
                            var contentType = FileUtility.GetFileContentType(file.FileStorageUrl);
                            var bytes = fileStorage.GetFileBytes(file.FileStorageUrl);
                            var contentPart = ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(bytes), contentType, ChatImageDetailLevel.Auto);
                            contentParts.Add(contentPart);
                        }
                        else if (!string.IsNullOrEmpty(file.FileUrl))
                        {
                            var uri = new Uri(file.FileUrl);
                            var contentPart = ChatMessageContentPart.CreateImagePart(uri, ChatImageDetailLevel.Auto);
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
                    var fnMessage = x as ToolChatMessage;
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
                        var toolCall = assistMessage.ToolCalls?.FirstOrDefault();
                        return toolCall != null ?
                            $"{AgentRole.Assistant}: Call function {toolCall?.FunctionName}({toolCall?.FunctionArguments})" :
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
