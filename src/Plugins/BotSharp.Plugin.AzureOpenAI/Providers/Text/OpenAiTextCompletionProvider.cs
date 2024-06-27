using BotSharp.Abstraction.MLTasks.Settings;
using BotSharp.Plugin.AzureOpenAI.Models;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace BotSharp.Plugin.AzureOpenAI.Providers.Text;

public class OpenAiTextCompletionProvider : TextCompletionProvider
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OpenAiTextCompletionProvider> _logger;

    public override string Provider => "openai";

    public OpenAiTextCompletionProvider(AzureOpenAiSettings settings,
        ILogger<OpenAiTextCompletionProvider> logger,
        IServiceProvider services) : base(settings, logger, services)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task<TextCompletionResponse> GetTextCompletion(string apiUrl, string apiKey, string prompt, float temperature, int maxTokens = 256)
    {
        try
        {
            var http = _services.GetRequiredService<IHttpClientFactory>();
            using var httpClient = http.CreateClient();
            AddHeader(httpClient, apiKey);

            var request = new OpenAiTextCompletionRequest
            {
                Model = _model,
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
            var rawResponse = await httpClient.SendAsync(httpRequest);
            rawResponse.EnsureSuccessStatusCode();

            var responseStr = await rawResponse.Content.ReadAsStringAsync();
            var response = JsonSerializer.Deserialize<TextCompletionResponse>(responseStr, _jsonOptions);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error when {Provider}-{_model} generating text... {ex.Message}");
            throw;
        }
    }

    protected override string BuildApiUrl(LlmModelSetting modelSetting)
    {
        var endpoint = modelSetting.Endpoint.EndsWith("/") ?
            modelSetting.Endpoint.Substring(0, modelSetting.Endpoint.Length - 1) : modelSetting.Endpoint;
        return endpoint ?? string.Empty;
    }

    protected override void AddHeader(HttpClient httpClient, string apiKey)
    {
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }
}
