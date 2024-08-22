using BotSharp.Plugin.Graph.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

namespace BotSharp.Plugin.Graph;

public class GraphDb : IGraphDb
{
    private readonly IServiceProvider _services;
    private readonly IHttpContextAccessor _context;
    private readonly GraphDbSettings _settings;
    private readonly ILogger<GraphDb> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        AllowTrailingCommas = true,
    };

    public GraphDb(
        IServiceProvider services,
        IHttpContextAccessor context,
        ILogger<GraphDb> logger,
        GraphDbSettings settings)
    {
        _services = services;
        _context = context;
        _logger = logger;
        _settings = settings;
    }

    public string Name => "Default";

    public async Task<GraphSearchData> Search(string query, GraphSearchOptions options)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            return new GraphSearchData();
        }

        var url = $"{_settings.BaseUrl}/query";
        var request = new GraphQueryRequest
        {
            Query = query,
            Method = options.Method
        };
        return await SendRequest(url, request);
    }

    private async Task<GraphSearchData> SendRequest(string url, GraphQueryRequest request)
    {
        var result = new GraphSearchData();
        var http = _services.GetRequiredService<IHttpClientFactory>();

        using (var client = http.CreateClient())
        {
            var uri = new Uri(url);
            try
            {
                var data = JsonSerializer.Serialize(request, _jsonOptions);
                var message = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = uri,
                    Content = new StringContent(data, Encoding.UTF8, MediaTypeNames.Application.Json)
                };

                AddHeaders(client);
                var rawResponse = await client.SendAsync(message);
                rawResponse.EnsureSuccessStatusCode();

                var responseStr = await rawResponse.Content.ReadAsStringAsync();
                result = JsonSerializer.Deserialize<GraphSearchData>(responseStr, _jsonOptions);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error when fetching Lessen GLM response (Endpoint: {url}). {ex.Message}\r\n{ex.InnerException}");
                return result;
            }
        }
    }

    private void AddHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("Authorization", $"{_context.HttpContext.Request.Headers["Authorization"]}");
        client.DefaultRequestHeaders.Add("Origin", $"{_context.HttpContext.Request.Headers["Origin"]}");
    }
}
