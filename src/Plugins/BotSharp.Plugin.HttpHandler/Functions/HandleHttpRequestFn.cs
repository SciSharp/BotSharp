using System.Net.Http;
using System.Net.Mime;
using BotSharp.Abstraction.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.HttpHandler.Functions;

public class HandleHttpRequestFn : IFunctionCallback
{
    public string Name => "util-http-handle_http_request";
    public string Indication => "Give me a second, I'm taking care of it!";

    private readonly IServiceProvider _services;
    private readonly ILogger<HandleHttpRequestFn> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
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
            message.Content = responseContent;
            return true;
        }
        catch (Exception ex)
        {
            var msg = $"Fail when sending http request. Url: {url}, method: {method}, content: {content}";
            _logger.LogError(ex, $"{msg}");
            message.Content = msg;
            return false;
        }
    }

    private async Task<HttpResponseMessage?> SendHttpRequest(string? url, string? method, string? content)
    {
        if (string.IsNullOrEmpty(url)) return null;

        using var client = _httpClientFactory.CreateClient();
        
        var (uri, request) = BuildHttpRequest(url, method, content);
        PrepareRequestHeaders(client, uri);

        var response = await client.SendAsync(request);
        if (response == null || !response.IsSuccessStatusCode)
        {
            _logger.LogWarning($"Response status code: {response?.StatusCode}");
        }

        return response;
    }

    private void PrepareRequestHeaders(HttpClient client, Uri uri)
    {
        var hooks = _services.GetServices<IHttpRequestHook>();
        foreach (var hook in hooks)
        {
            hook.OnAddHttpHeaders(client.DefaultRequestHeaders, uri);
        }
    }

    private (Uri, HttpRequestMessage) BuildHttpRequest(string url, string? method, string? content)
    {
        var httpMethod = GetHttpMethod(method);
        StringContent httpContent;

        var requestUrl = url;
        if (httpMethod == HttpMethod.Get)
        {
            httpContent = BuildHttpContent("{}");
            requestUrl = BuildQuery(url, content);
        }
        else
        {
            httpContent = BuildHttpContent(content);
        }

        if (!Uri.TryCreate(requestUrl, UriKind.Absolute, out var uri))
        {
            var settings = _services.GetRequiredService<HttpHandlerSettings>();
            var baseUri = new Uri(settings.BaseAddress);
            uri = new Uri(baseUri, requestUrl);
        }
        
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
            case "POST":
                matchMethod = HttpMethod.Post;
                break;
            case "DELETE":
                matchMethod = HttpMethod.Delete;
                break;
            case "PUT":
                matchMethod = HttpMethod.Put;
                break;
            case "PATCH":
                matchMethod = HttpMethod.Patch;
                break;
            default:
                matchMethod = HttpMethod.Get;
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
        
        return new StringContent(str, Encoding.UTF8, MediaTypeNames.Application.Json);
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

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            return response.ReasonPhrase ?? "http call has error occurred.";
        }

        return await response.Content.ReadAsStringAsync();
    }
}
