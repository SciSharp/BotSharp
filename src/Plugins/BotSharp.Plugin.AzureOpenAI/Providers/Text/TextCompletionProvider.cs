using BotSharp.Abstraction.MLTasks.Settings;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace BotSharp.Plugin.AzureOpenAI.Providers.Text;

public class TextCompletionProvider : ITextCompletion
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TextCompletionProvider> _logger;
    private readonly AzureOpenAiSettings _settings;
    protected string _model;

    protected readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        AllowTrailingCommas = true,
    };

    public virtual string Provider => "azure-openai";

    public TextCompletionProvider(
        AzureOpenAiSettings settings,
        ILogger<TextCompletionProvider> logger,
        IServiceProvider services)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }

    public async Task<string> GetCompletion(string text, string agentId, string messageId)
    {
        var contentHooks = _services.GetServices<IContentGeneratingHook>().ToList();

        // Before chat completion hook
        var agent = new Agent()
        {
            Id = agentId,
        };
        var message = new RoleDialogModel(AgentRole.User, text)
        {
            CurrentAgentId = agentId,
            MessageId = messageId
        };

        Task.WaitAll(contentHooks.Select(hook =>
            hook.BeforeGenerating(agent,
                new List<RoleDialogModel>
                {
                    message
                })).ToArray());

        var state = _services.GetRequiredService<IConversationStateService>();
        var temperature = float.Parse(state.GetState("temperature", "0.0"));

        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var modelSetting = settingsService.GetSetting(Provider, _model);
        var apiUrl = BuildApiUrl(modelSetting);
        var apiKey = modelSetting.ApiKey;
        var response = await GetTextCompletion(apiUrl, apiKey, text, temperature);

        // OpenAI
        var completion = "";
        foreach (var t in response.Choices)
        {
            completion += t?.Text ?? string.Empty;
        };

        // After chat completion hook
        var responseMessage = new RoleDialogModel(AgentRole.Assistant, completion)
        {
            CurrentAgentId = agentId,
            MessageId = messageId
        };
        Task.WaitAll(contentHooks.Select(hook =>
            hook.AfterGenerated(responseMessage, new TokenStatsModel
            {
                Prompt = text,
                Provider = Provider,
                Model = _model,
                PromptCount = response.Usage?.PromptTokens ?? default,
                CompletionCount = response.Usage?.CompletionTokens ?? default
            })).ToArray());

        return completion.Trim();
    }

    public void SetModelName(string model)
    {
        _model = model;
    }

    private async Task<TextCompletionResponse> GetTextCompletion(string apiUrl, string apiKey, string prompt, float temperature, int maxTokens = 256)
    {
        try
        {
            var http = _services.GetRequiredService<IHttpClientFactory>();
            using var httpClient = http.CreateClient();
            AddHeader(httpClient, apiKey);

            var request = new TextCompletionRequest
            {
                Prompt = prompt,
                MaxTokens = maxTokens,
                Temperature = temperature
            };
            var data = JsonSerializer.Serialize(request, _jsonOptions);
            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(apiUrl),
                Content = new StringContent(data, Encoding.UTF8, MediaTypeNames.Application.Json)
            };

            var httpResponse = await httpClient.SendAsync(httpRequest);
            httpResponse.EnsureSuccessStatusCode();
            var responseStr = await httpResponse.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<TextCompletionResponse>(responseStr, _jsonOptions);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when {Provider}-{_model} generating text... {ex.Message}");
            throw;
        }
    }

    private string BuildApiUrl(LlmModelSetting modelSetting)
    {
        var url = string.Empty;
        var endpoint = modelSetting.Endpoint.EndsWith("/") ?
            modelSetting.Endpoint.Substring(0, modelSetting.Endpoint.Length - 1) : modelSetting.Endpoint;

        url = $"{endpoint}/openai/deployments/{_model}/completions?api-version=2023-09-15-preview";
        return url;
    }

    private void AddHeader(HttpClient httpClient, string apiKey)
    {
        httpClient.DefaultRequestHeaders.Add("api-key", $"{apiKey}");
    }
}
