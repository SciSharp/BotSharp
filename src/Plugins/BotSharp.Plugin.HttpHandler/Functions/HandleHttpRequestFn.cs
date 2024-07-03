using System.Net.Http;
using BotSharp.Plugin.HttpHandler.LlmContexts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.HttpHandler.Functions;

public class HandleHttpRequestFn : IFunctionCallback
{
    public string Name => "handle_http_request";
    public string Indication => "Handling http request";

    private readonly IServiceProvider _services;
    private readonly ILogger<HandleHttpRequestFn> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _context;
    private readonly BotSharpOptions _options;

    public HandleHttpRequestFn(IServiceProvider services,
        ILogger<HandleHttpRequestFn> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor context,
        BotSharpOptions options)
    {
        _services = services;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _context = context;
        _options = options;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<LlmContextIn>(message.FunctionArgs, _options.JsonSerializerOptions);
        var url = args?.RequestUrl;
        var method = args?.HttpMethod;
        var content = args?.RequestContent;

        try
        {
            var response = await SendHttpRequest(url, method, content);
            var responseContent = await HandleHttpResponse(response);
            message.RichContent = BuildRichContent(responseContent);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            var msg = $"Fail when sending http request. Url: {url}, method: {method}, content: {content}";
            _logger.LogWarning($"{msg}\n(Error: {ex.Message})");
            message.RichContent = BuildRichContent($"{msg}");
            return await Task.FromResult(false);
        }
    }

    private async Task<HttpResponseMessage?> SendHttpRequest(string? url, string? method, string? content)
    {
        if (string.IsNullOrEmpty(url)) return null;

        var settings = _services.GetRequiredService<HttpSettings>();
        using var client = _httpClientFactory.CreateClient();
        AddRequestHeaders(client);

        var (uri, request) = BuildHttpRequest(url, method, content);
        if (string.IsNullOrEmpty(uri.Host))
        {
            client.BaseAddress = new Uri(settings.BaseAddress);
        }

        var response = await client.SendAsync(request);

        if (response == null || !response.IsSuccessStatusCode)
        {
            throw new Exception($"Status code: {response?.StatusCode}");
        }

        return response;
    }

    private void AddRequestHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("Authorization", $"{_context.HttpContext.Request.Headers["Authorization"]}");

        var settings = _services.GetRequiredService<HttpSettings>();
        var origin = !string.IsNullOrEmpty(settings.Origin) ? settings.Origin : $"{_context.HttpContext.Request.Headers["Origin"]}";
        if (!string.IsNullOrEmpty(origin))
        {
            client.DefaultRequestHeaders.Add("Origin", origin);
        }
    }

    private (Uri, HttpRequestMessage) BuildHttpRequest(string url, string? method, string? content)
    {
        var httpMethod = GetHttpMethod(method);
        StringContent httpContent;

        if (httpMethod == HttpMethod.Get)
        {
            httpContent = BuildHttpContent("{}");
        }
        else
        {
            httpContent = BuildHttpContent(content);
        }

        var requestUrl = BuildQuery(url, content);
        var uri = new Uri(requestUrl);
        return (uri, new HttpRequestMessage
        {
            RequestUri = uri,
            Method = httpMethod,
            Content = httpContent
        });
    }

    private HttpMethod GetHttpMethod(string? method)
    {
        var localMethod = method?.Trim()?.ToUpper();
        HttpMethod matchMethod;

        switch (localMethod)
        {
            case "GET":
                matchMethod = HttpMethod.Get;
                break;
            case "DELETE":
                matchMethod = HttpMethod.Delete;
                break;
            case "PUT":
                matchMethod = HttpMethod.Put;
                break;
            case "Patch":
                matchMethod = HttpMethod.Patch;
                break;
            default:
                matchMethod = HttpMethod.Post;
                break;
        }
        return matchMethod;
    }

    private StringContent BuildHttpContent(string? content)
    {
        var str = string.Empty;
        try
        {
            var json = JsonSerializer.Deserialize<JsonDocument>(content ?? "{}", _options.JsonSerializerOptions);
            str = JsonSerializer.Serialize(json, _options.JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when build http content: {content}\n(Error: {ex.Message})");
        }
        
        return new StringContent(str, Encoding.UTF8, "application/json");
    }

    private string BuildQuery(string url, string? content)
    {
        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(content)) return url;

        try
        {
            var queries = new List<string>();
            var json = JsonSerializer.Deserialize<JsonDocument>(content, _options.JsonSerializerOptions);
            var root = json.RootElement;
            foreach (var prop in root.EnumerateObject())
            {
                var name = prop.Name.Trim();
                var value = prop.Value.ToString().Trim();
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
                {
                    continue;
                }

                queries.Add($"{name}={value}");
            }

            if (!queries.IsNullOrEmpty())
            {
                url += $"?{string.Join('&', queries)}";
            }
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error when building url query. Url: {url}, Content: {content}\n(Error: {ex.Message})");
            return url;
        }
    }

    private async Task<string> HandleHttpResponse(HttpResponseMessage? response)
    {
        if (response == null) return string.Empty;

        return await response.Content.ReadAsStringAsync();
    }

    private RichContent<IRichMessage> BuildRichContent(string? content)
    {
        var state = _services.GetRequiredService<IConversationStateService>();

        var text = !string.IsNullOrEmpty(content) ? content : "Cannot get any response from the http request.";
        return new RichContent<IRichMessage>
        {
            Recipient = new Recipient { Id = state.GetConversationId() },
            Editor = EditorTypeEnum.Text,
            Message = new TextMessage(text)
        };
    }
}
