using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;

namespace BotSharp.Plugin.FloxiAI.Providers;

/// <summary>
/// Default transport: posts to the OpenAI-compatible endpoint configured for the model
/// (LlmProviders -> floxi-ai -> Endpoint), falling back to <see cref="FloxiAiConstants.DefaultEndpoint"/>,
/// authenticating with that model's ApiKey. A whole reply and a server-sent event stream are read off
/// the same route; which one the backend produces follows the "stream" flag in the body.
/// </summary>
public class HttpFloxiChatTransport : IFloxiChatTransport
{
    private readonly IServiceProvider _services;
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpFloxiChatTransport(IServiceProvider services, IHttpClientFactory httpClientFactory)
    {
        _services = services;
        _httpClientFactory = httpClientFactory;
    }

    public bool SupportsStreaming => true;

    public async Task<FloxiChatTransportResult> SendChatAsync(string model, string payload, CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(model, payload);

        var http = _httpClientFactory.CreateClient(nameof(HttpFloxiChatTransport));
        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new FloxiChatTransportResult
        {
            StatusCode = (int)response.StatusCode,
            Body = body
        };
    }

    public async IAsyncEnumerable<string> SendChatStreamAsync(
        string model,
        string payload,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var request = BuildRequest(model, payload);

        var http = _httpClientFactory.CreateClient(nameof(HttpFloxiChatTransport));

        // ResponseHeadersRead, otherwise SendAsync buffers the whole response and every delta arrives
        // at once — the request would stream and the caller would not.
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Model '{model}' on {FloxiAiConstants.ProviderName} rejected the streaming request " +
                $"(status {(int)response.StatusCode}): {error}");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (true)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null)
            {
                break;
            }

            // Event separators, comment keep-alives and the "event:" field are all skipped: the chunk
            // objects are the only thing the provider needs off the wire.
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var data = line[5..].Trim();
            if (data.Length == 0)
            {
                continue;
            }

            if (data == "[DONE]")
            {
                break;
            }

            yield return data;
        }
    }

    private HttpRequestMessage BuildRequest(string model, string payload)
    {
        var settingsService = _services.GetRequiredService<ILlmProviderService>();
        var settings = settingsService.GetSetting(FloxiAiConstants.ProviderName, model);

        // The public network is the default, so a model that only carries an ApiKey still works; an
        // Endpoint is only needed to point at a different deployment.
        var endpoint = FloxiAiConstants.DefaultEndpoint;

        var request = new HttpRequestMessage(HttpMethod.Post, BuildChatUrl(endpoint))
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(settings?.ApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        }

        return request;
    }

    /// <summary>
    /// Accepts either the base url ("https://host/v1") or the full route, so a setting copied from an
    /// SDK config and one copied from a curl command both work.
    /// </summary>
    private static string BuildChatUrl(string endpoint)
    {
        var trimmed = endpoint.TrimEnd('/');
        return trimmed.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : $"{trimmed}/chat/completions";
    }
}
