using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Hooks;
using BotSharp.Abstraction.Loggers;
using BotSharp.Abstraction.MessageHub.Models;
using BotSharp.Core.Infrastructures.Streams;
using BotSharp.Core.MessageHub;
using Microsoft.AspNetCore.SignalR;
using static LLama.Common.ChatHistory;
using static System.Net.Mime.MediaTypeNames;

namespace BotSharp.Plugin.LLamaSharp.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly LlamaSharpSettings _settings;
    private List<string> renderedInstructions = [];
    private string _model;

    public ChatCompletionProvider(IServiceProvider services,
        ILogger<ChatCompletionProvider> logger,
        LlamaSharpSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public string Provider => "llama-sharp";
    public string Model => _model;

    public async Task<RoleDialogModel> GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        // Before chat completion hook
        foreach (var hook in hooks)
        {
            await hook.BeforeGenerating(agent, conversations);
        }

        var content = string.Join("\r\n", conversations.Select(x => $"{x.Role}: {x.Content}")).Trim();
        content += $"\r\n{AgentRole.Assistant}: ";

        var llama = _services.GetRequiredService<LlamaAiModel>();
        llama.LoadModel(_model);
        var executor = llama.GetStatelessExecutor();

        var inferenceParams = new InferenceParams()
        {
            AntiPrompts = new List<string> { $"{AgentRole.User}:", "[/INST]" },
            MaxTokens = 128
        };

        string totalResponse = "";

        var agentService = _services.GetRequiredService<IAgentService>();
        var instruction = agentService.RenderedInstruction(agent);
        var prompt = instruction + "\r\n" + content;

        await foreach(var text in Spinner(executor.InferAsync(prompt, inferenceParams)))
        {
            Console.Write(text);
            totalResponse += text;
        }

        foreach (var anti in inferenceParams.AntiPrompts)
        {
            totalResponse = totalResponse.Replace(anti, "").Trim();
        }

        var msg = new RoleDialogModel(AgentRole.Assistant, totalResponse)
        {
            CurrentAgentId = agent.Id,
            RenderedInstruction = instruction
        };

        // After chat completion hook
        foreach (var hook in hooks)
        {
            await hook.AfterGenerated(msg, new TokenStatsModel
            {
                Prompt = prompt,
                Provider = Provider,
                Model = _model
            });
        }

        return msg;
    }

    public async IAsyncEnumerable<string> Spinner(IAsyncEnumerable<string> source)
    {
        var enumerator = source.GetAsyncEnumerator();

        var characters = new[] { '|', '/', '-', '\\' };

        while (true)
        {
            var next = enumerator.MoveNextAsync();

            while (!next.IsCompleted)
            {
                await Task.Delay(75);
            }

            if (!next.Result)
                break;
            yield return enumerator.Current;
        }
    }

    public async Task<bool> GetChatCompletionsAsync(Agent agent,
        List<RoleDialogModel> conversations,
        Func<RoleDialogModel, Task> onMessageReceived,
        Func<RoleDialogModel, Task> onFunctionExecuting)
    {
        var content = string.Join("\r\n", conversations.Select(x => $"{x.Role}: {x.Content}")).Trim();
        content += $"\r\n{AgentRole.Assistant}: ";

        var state = _services.GetRequiredService<IConversationStateService>();
        var model = state.GetState("model", _settings.DefaultModel);

        var llama = _services.GetRequiredService<LlamaAiModel>();
        llama.LoadModel(model);
        var executor = llama.GetStatelessExecutor();

        var inferenceParams = new InferenceParams()
        {
            AntiPrompts = new List<string> { $"{AgentRole.User}:", "[/INST]" },
            MaxTokens = 64
        };

        string totalResponse = "";

        var prompt = agent.Instruction + "\r\n" + content;

        var convSetting = _services.GetRequiredService<ConversationSetting>();
        if (convSetting.ShowVerboseLog)
        {
            _logger.LogInformation(prompt);
        }

        await foreach (var response in executor.InferAsync(prompt, inferenceParams))
        {
            Console.Write(response);
            totalResponse += response;
        }

        foreach (var anti in inferenceParams.AntiPrompts)
        {
            totalResponse = totalResponse.Replace(anti, "").Trim();
        }

        var msg = new RoleDialogModel(AgentRole.Assistant, totalResponse)
        {
            CurrentAgentId = agent.Id,
            RenderedInstruction = agent.Instruction
        };

        // Text response received
        await onMessageReceived(msg);

        return true;
    }

    public async Task<RoleDialogModel> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations)
    {
        var state = _services.GetRequiredService<IConversationStateService>();
        var model = state.GetState("model", "llama-2-7b-chat.Q8_0");

        var llama = _services.GetRequiredService<LlamaAiModel>();
        llama.LoadModel(model);

        var executor = new StatelessExecutor(llama.Model, llama.Params);
        var inferenceParams = new InferenceParams() { AntiPrompts = new List<string> { $"{AgentRole.User}:" }, MaxTokens = 64 };

        var convSetting = _services.GetRequiredService<ConversationSetting>();
        if (convSetting.ShowVerboseLog)
        {
            _logger.LogInformation(agent.Instruction);
        }

        var hub = _services.GetRequiredService<MessageHub<HubObserveData<RoleDialogModel>>>();
        var conv = _services.GetRequiredService<IConversationService>();
        var messageId = conversations.LastOrDefault()?.MessageId ?? string.Empty;

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

        using var textStream = new RealtimeTextStream();
        var responseMessage = new RoleDialogModel(AgentRole.Assistant, string.Empty)
        {
            CurrentAgentId = agent.Id,
            MessageId = messageId
        };

        await foreach (var response in executor.InferAsync(agent.Instruction, inferenceParams))
        {
            Console.Write(response);
            textStream.Collect(response);

            var content = new RoleDialogModel(AgentRole.Assistant, response)
            {
                CurrentAgentId = agent.Id,
                MessageId = messageId
            };
            hub.Push(new()
            {
                EventName = ChatEvent.OnReceiveLlmStreamMessage,
                RefId = conv.ConversationId,
                Data = content
            });
        }

        responseMessage = new RoleDialogModel(AgentRole.Assistant, textStream.GetText())
        {
            CurrentAgentId = agent.Id,
            MessageId = messageId,
            IsStreaming = true
        };

        hub.Push(new()
        {
            EventName = ChatEvent.AfterReceiveLlmStreamMessage,
            RefId = conv.ConversationId,
            Data = responseMessage
        });

        return responseMessage;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
