namespace BotSharp.Plugin.LLamaSharp.Providers;

public class TextCompletionProvider : ITextCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    private readonly LlamaSharpSettings _settings;
    private readonly ITokenStatistics _tokenStatistics;
    private string _model;
    public string Provider => "llama-sharp";

    public TextCompletionProvider(IServiceProvider services,
        ILogger<TextCompletionProvider> logger,
        LlamaSharpSettings settings,
        ITokenStatistics tokenStatistics)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
        _tokenStatistics = tokenStatistics;
    }

    public async Task<string> GetCompletion(string text)
    {
        var hooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.BeforeGenerating(new Agent(), new List<RoleDialogModel> { new RoleDialogModel(AgentRole.User, text) })).ToArray());

        var llama = _services.GetRequiredService<LlamaAiModel>();
        llama.LoadModel(_model);

        var executor = new InstructExecutor(llama.Model.CreateContext(llama.Params));
        var inferenceParams = new InferenceParams() { Temperature = 0.5f, MaxTokens = 128 };

        _tokenStatistics.StartTimer();
        string completion = "";
        await foreach (var response in executor.InferAsync(text, inferenceParams))
        {
            Console.Write(response);
            completion += response;
        }
        _tokenStatistics.StopTimer();

        // After chat completion hook
        Task.WaitAll(hooks.Select(hook =>
            hook.AfterGenerated(new RoleDialogModel(AgentRole.Assistant, completion), new TokenStatsModel
            {
                Model = _model
            })).ToArray());

        return completion;
    }

    public void SetModelName(string model)
    {
        _model = model;
    }
}
