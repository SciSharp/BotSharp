using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace BotSharp.Core.Rules.Actions;

public sealed class HttpRuleAction : IRuleAction
{
    private readonly IServiceProvider _services;
    private readonly ILogger<HttpRuleAction> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly JsonSerializerOptions _defaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowTrailingCommas = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public HttpRuleAction(
        IServiceProvider services,
        ILogger<HttpRuleAction> logger,
        IHttpClientFactory httpClientFactory)
    {
        _services = services;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public string Name => "http_request";

    public async Task<RuleActionResult> ExecuteAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleActionContext context)
    {
        try
        {
            var httpMethod = GetHttpMethod(context);
            if (httpMethod == null)
            {
                var errorMsg = $"HTTP method is not supported in agent rule {agent.Name}-{trigger.Name}";
                _logger.LogWarning(errorMsg);
                return RuleActionResult.Failed(errorMsg);
            }

            // Build the full URL
            var fullUrl = BuildUrl(context);

            using var client = _httpClientFactory.CreateClient();

            // Add headers
            AddHttpHeaders(client, context);

            // Create request
            var request = new HttpRequestMessage(httpMethod, fullUrl);

            // Add request body if provided
            var requestBodyStr = GetHttpRequestBody(context);
            if (!string.IsNullOrEmpty(requestBodyStr))
            {
                request.Content = new StringContent(requestBodyStr, Encoding.UTF8, MediaTypeNames.Application.Json);
            }

            _logger.LogInformation("Executing HTTP rule action for agent {AgentId}, URL: {Url}, Method: {Method}",
                agent.Id, fullUrl, httpMethod);

            // Send request
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("HTTP rule action executed successfully for agent {AgentId}, Status: {StatusCode}, Response: {Response}",
                    agent.Id, response.StatusCode, responseContent);

                return new RuleActionResult
                {
                    Success = true,
                    Response = responseContent,
                    Data = new()
                    {
                        ["http_response"] = responseContent
                    }
                };
            }
            else
            {
                var errorMsg = $"HTTP request failed with status code {response.StatusCode}: {responseContent}";
                _logger.LogWarning(errorMsg);
                return RuleActionResult.Failed(errorMsg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing HTTP rule action for agent {AgentId} and trigger {TriggerName}",
                agent.Id, trigger.Name);
            return RuleActionResult.Failed(ex.Message);
        }
    }

    private string BuildUrl(RuleActionContext context)
    {
        var url = context.Parameters.TryGetValueOrDefault<string>("http_url");
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentNullException("Unable to find http_url in context");
        }

        // Fill in placeholders in url
        foreach (var param in context.Parameters)
        {
            var value = param.Value?.ToString();
            if (string.IsNullOrEmpty(value))
            {
                continue; 
            }
            url = url.Replace($"{{{param.Key}}}", value);
        }

        // Add query parameters
        var queryParams = context.Parameters.TryGetValueOrDefault<IEnumerable<KeyValue>>("http_query_params");
        if (!queryParams.IsNullOrEmpty())
        {
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);

            // Add new query params
            foreach (var kv in queryParams!.Where(x => x.Value != null))
            {
                query[kv.Key] = kv.Value!;
            }

            // Assign merged query back
            builder.Query = query.ToString();
            url = builder.ToString();
        }

        _logger.LogInformation("HTTP url after filling: {Url}", url);
        return url;
    }

    private HttpMethod? GetHttpMethod(RuleActionContext context)
    {
        var method = context.Parameters.TryGetValueOrDefault("http_method", string.Empty);
        var innerMethod = method?.Trim()?.ToUpper();
        HttpMethod? matchMethod = null;

        switch (innerMethod)
        {
            case "GET":
                matchMethod = HttpMethod.Get;
                break;
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
                break;

        }

        return matchMethod;
    }

    private void AddHttpHeaders(HttpClient client, RuleActionContext context)
    {
        var headerParams = context.Parameters.TryGetValueOrDefault<IEnumerable<KeyValue>>("http_request_headers");
        if (!headerParams.IsNullOrEmpty())
        {
            foreach (var header in headerParams!)
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }

    private string? GetHttpRequestBody(RuleActionContext context)
    {
        var body = context.Parameters.GetValueOrDefault("http_request_body");
        if (body == null)
        {
            return null;
        }

        return JsonSerializer.Serialize(body, context.JsonOptions ?? _defaultJsonOptions);
    }
}

