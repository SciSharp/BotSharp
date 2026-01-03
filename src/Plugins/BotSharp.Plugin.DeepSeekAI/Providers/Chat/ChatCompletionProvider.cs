#pragma warning disable OPENAI001
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Files;
using BotSharp.Abstraction.Files.Models;
using BotSharp.Abstraction.Files.Utilities;
using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Core.Infrastructures.Streams;
using BotSharp.Core.MessageHub;
using BotSharp.Plugin.DeepSeek.Providers;
using Microsoft.Extensions.Logging;
using DeepSeek.Core;
using DeepSeek.Core.Models;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace BotSharp.Plugin.DeepSeekAI.Providers.Chat;

public class ChatCompletionProvider : IChatCompletion
{
    protected readonly IServiceProvider _services;
    protected readonly ILogger<ChatCompletionProvider> _logger;
    private List<string> renderedInstructions = [];


    protected string _model;
    public virtual string Provider => "deepseek-ai";
    public string Model => _model;

    public ChatCompletionProvider(
        IServiceProvider services,
        ILogger<ChatCompletionProvider> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);

        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var (prompt, request) = BuildRequest(agent, conversations, stream: false);
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var response = await client.ChatAsync(request, CancellationToken.None);

        var message = response?.Choices?.FirstOrDefault()?.Message;
        var responseMessage = MapMessage(agent, conversations, message);

        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model
            });
        }

        return responseMessage;
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived, Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var hooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);
        foreach (var hook in hooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var (prompt, request) = BuildRequest(agent, conversations, stream: false);
        var client = ProviderHelper.GetClient(Provider, _model, _services);
        var response = await client.ChatAsync(request, CancellationToken.None);
        var message = response?.Choices?.FirstOrDefault()?.Message;
        var mapped = MapMessage(agent, conversations, message);

        foreach (var hook in hooks)
        {
            await hook.AfterGenerated(mapped, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model
            });
        }

        if (mapped.Role == AgentRole.Function)
        {
            await onFunctionExecuting(mapped);
        }
        else
        {
            await onMessageReceived(mapped);
        }

        return true;
    }

    public async Task<RoleDialogModel> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations)
    {
        var (prompt, request) = BuildRequest(agent, conversations, stream: true);
        var client = ProviderHelper.GetClient(Provider, _model, _services);

        var hub = _services.GetRequiredService<MessageHub<HubObserveData<RoleDialogModel>>>();
        var conv = _services.GetRequiredService<IConversationService>();
        var messageId = conversations.LastOrDefault()?.MessageId ?? string.Empty;

        var contentHooks = _services.GetHooks<IContentGeneratingHook>(agent.Id);
        foreach (var hook in contentHooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        hub.Push(new()
        {
            EventName = ChatEvent.BeforeReceiveLlmStreamMessage,
            RefId = conv.ConversationId,
            Data = new RoleDialogModel(AgentRole.Assistant, string.Empty)
            {
                CurrentAgentId = agent.Id,
                MessageId = messageId
            }
        });

        var textStream = new RealtimeTextStream();
        var responseMessage = new RoleDialogModel(AgentRole.Assistant, string.Empty)
        {
            CurrentAgentId = agent.Id,
            MessageId = messageId,
            IsStreaming = true
        };

        var choices = client.ChatStreamAsync(request, CancellationToken.None);
        if (choices != null)
        {
            await foreach (var choice in choices)
            {
                var delta = choice?.Delta?.Content;
                if (!string.IsNullOrEmpty(delta))
                {
                    textStream.Collect(delta);
#if DEBUG
                    _logger.LogCritical($"Content update: {delta}");
#endif
                    hub.Push(new()
                    {
                        EventName = ChatEvent.OnReceiveLlmStreamMessage,
                        RefId = conv.ConversationId,
                        Data = new RoleDialogModel(AgentRole.Assistant, delta)
                        {
                            CurrentAgentId = agent.Id,
                            MessageId = messageId, 
                            ReasoningContent = choice?.Delta?.ReasoningContent ?? string.Empty
                        }
                    });
                }
            }
        }

        responseMessage.Content = textStream.GetText();

        hub.Push(new()
        {
            EventName = ChatEvent.AfterReceiveLlmStreamMessage,
            RefId = conv.ConversationId,
            Data = responseMessage,
        });

        foreach (var hook in contentHooks)
        {
            await hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model
            });
        }

        return responseMessage;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    private RoleDialogModel MapMessage(Agent agent, List<RoleDialogModel> conversations, Message? message)
    {
        var lastMessageId = conversations.LastOrDefault()?.MessageId ?? string.Empty;
        if (message?.ToolCalls != null && message.ToolCalls.Count > 0)
        {
            var tool = message.ToolCalls.First();
            return new RoleDialogModel(AgentRole.Function, message.Content ?? string.Empty)
            {
                CurrentAgentId = agent.Id,
                MessageId = lastMessageId,
                ToolCallId = tool.Id,
                FunctionName = tool.Function?.Name,
                FunctionArgs = tool.Function?.Arguments?.ToString(),
                RenderedInstruction = string.Join("\r\n", renderedInstructions),
                ReasoningContent = message.ReasoningContent ?? string.Empty
            };
        }

        return new RoleDialogModel(AgentRole.Assistant, message?.Content ?? string.Empty)
        {
            CurrentAgentId = agent.Id,
            MessageId = lastMessageId,
            RenderedInstruction = string.Join("\r\n", renderedInstructions),
            ReasoningContent = message.ReasoningContent ?? string.Empty
        };
    }

    private (string prompt, ChatRequest request) BuildRequest(Agent agent, List<RoleDialogModel> conversations, bool stream)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var state = _services.GetRequiredService<IConversationStateService>();
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(Provider, _model);
        renderedInstructions = [];

        var messages = new List<Message>();

        var renderData = agentService.CollectRenderData(agent);
        var (instruction, functions) = agentService.PrepareInstructionAndFunctions(agent, renderData);
        if (!string.IsNullOrWhiteSpace(instruction))
        {
            renderedInstructions.Add(instruction);
            messages.Add(Message.NewSystemMessage(instruction));
        }

        if (!string.IsNullOrEmpty(agent.Knowledges))
        {
            messages.Add(Message.NewSystemMessage(agent.Knowledges));
        }

        foreach (var dialog in conversations)
        {
            if (dialog.Role == AgentRole.User)
            {
                messages.Add(Message.NewUserMessage(dialog.LlmContent));
            }
            else if (dialog.Role == AgentRole.Assistant)
            {
                messages.Add(Message.NewAssistantMessage(dialog.LlmContent));
            }
        }

        var tools = BuildTools(agent, functions, renderData);

        var temperature = float.TryParse(state.GetState("temperature"), out var tempVal) ? tempVal : 0.0f;
        var maxTokens = int.TryParse(state.GetState("max_tokens"), out var tokens)
                        ? tokens
                        : agent.LlmConfig?.MaxOutputTokens ?? LlmConstant.DEFAULT_MAX_OUTPUT_TOKEN;

        var request = new ChatRequest
        {
            Model = string.IsNullOrWhiteSpace(_model) ? DeepSeekModels.ChatModel : _model,
            Messages = messages,
            Stream = stream,
            Temperature = temperature,
            MaxTokens = maxTokens,
            Tools = tools
        };

        var prompt = string.Join("\n", messages.Select(m => m.Content));
        return (prompt, request);
    }

    private List<Tool> BuildTools(Agent agent, IEnumerable<FunctionDef> functions, IDictionary<string, object> renderData)
    {
        var agentService = _services.GetRequiredService<IAgentService>();
        var tools = new List<Tool>();

        foreach (var function in functions)
        {
            if (!agentService.RenderFunction(agent, function, renderData))
            {
                continue;
            }

            var property = agentService.RenderFunctionProperty(agent, function, renderData);
            JsonNode? parameters = null;
            if (property != null)
            {
                parameters = JsonSerializer.SerializeToNode(property);
            }

            tools.Add(new Tool
            {
                Function = new RequestFunction
                {
                    Name = function.Name,
                    Description = function.Description,
                    Parameters = parameters
                }
            });
        }

        return tools;
    }
}
