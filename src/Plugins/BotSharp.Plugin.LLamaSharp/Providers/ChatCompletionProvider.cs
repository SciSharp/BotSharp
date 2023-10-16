namespace BotSharp.Plugin.LLamaSharp.Providers;

public class ChatCompletionProvider : IChatCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly LlamaSharpSettings _settings;
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

    public RoleDialogModel GetChatCompletions(Agent agent, List<RoleDialogModel> conversations)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.BeforeGenerating(agent, conversations)).ToArray());

        var content = string.Join("\r\n", conversations.Select(x => $"{x.Role}: {x.Content}")).Trim();
        content += $"\r\n{AgentRole.Assistant}: ";

        var llama = _services.GetRequiredService<LlamaAiModel>();
        llama.LoadModel(_model);
        var executor = llama.GetStatelessExecutor();

        var inferenceParams = new InferenceParams()
        {
            Temperature = 0.1f,
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

        foreach (var response in executor.Infer(prompt, inferenceParams))
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
            CurrentAgentId = agent.Id
        };

        // After chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(msg, new TokenStatsModel
            {
                Model = _model
            })).ToArray());

        return msg;
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
            Temperature = 0.1f,
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

        foreach (var response in executor.Infer(prompt, inferenceParams))
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
            CurrentAgentId = agent.Id
        };

        // Text response received
        await onMessageReceived(msg);

        return true;
    }

    public async Task<bool> GetChatCompletionsStreamingAsync(Agent agent, List<RoleDialogModel> conversations, Func<RoleDialogModel, Task> onMessageReceived)
    {
        string totalResponse = "";
        var content = string.Join("\r\n", conversations.Select(x => $"{x.Role}: {x.Content}")).Trim();
        content += $"\r\n{AgentRole.Assistant}: ";

        var state = _services.GetRequiredService<IConversationStateService>();
        var model = state.GetState("model", "llama-2-7b-chat.Q8_0");

        var llama = _services.GetRequiredService<LlamaAiModel>();
        llama.LoadModel(model);

        var executor = new StatelessExecutor(llama.Model, llama.Params);
        var inferenceParams = new InferenceParams() { Temperature = 1.0f, AntiPrompts = new List<string> { $"{AgentRole.User}:" }, MaxTokens = 64 };

        var convSetting = _services.GetRequiredService<ConversationSetting>();
        if (convSetting.ShowVerboseLog)
        {
            _logger.LogInformation(agent.Instruction);
        }

        foreach (var response in executor.Infer(agent.Instruction, inferenceParams))
        {
            Console.Write(response);
            totalResponse += response;
        }

        return true;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
