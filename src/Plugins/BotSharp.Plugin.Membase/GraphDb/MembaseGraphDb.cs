using Polly;
using Polly.Timeout;
using Refit;

namespace BotSharp.Plugin.Membase.GraphDb;

public partial class MembaseGraphDb : IGraphDb
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MembaseGraphDb> _logger;
    private readonly IMembaseApi _membaseApi;

    public MembaseGraphDb(
        IServiceProvider services,
        ILogger<MembaseGraphDb> logger,
        IMembaseApi membaseApi)
    {
        _services = services;
        _logger = logger;
        _membaseApi = membaseApi;
    }

    public string Provider => "membase";

    private const int RetryCount = 3;

    private AsyncPolicy BuildRetryPolicy()
    {
        var settings = _services.GetRequiredService<MembaseSettings>();
        var timeoutSeconds = (double)settings.TimeoutSecond / RetryCount;

        var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(timeoutSeconds));

        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .Or<ApiException>(ex => ex.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(
                retryCount: RetryCount,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (ex, timespan, retryAttempt, _) =>
                {
                    _logger.LogWarning(ex,
                        "CypherQueryAsync retry {RetryAttempt}/{MaxRetries} after {Delay}s. Exception: {Message}",
                        retryAttempt, RetryCount, timespan.TotalSeconds, ex.Message);
                });

        return Policy.WrapAsync(retryPolicy, timeoutPolicy);
    }

    public async Task<GraphQueryResult> ExecuteQueryAsync(string query, GraphQueryExecuteOptions? options = null)
    {
        if (string.IsNullOrEmpty(options?.GraphId))
        {
            throw new ArgumentException($"Please provide a valid {Provider} graph id.");
        }

        var args = options?.Arguments ?? new();
        var argLogs = JsonSerializer.Serialize(args, BotSharpOptions.defaultJsonOptions);

        try
        {
            var retryPolicy = BuildRetryPolicy();
            var response = await retryPolicy.ExecuteAsync(() =>
                _membaseApi.CypherQueryAsync(options!.GraphId, new CypherQueryRequest
                {
                    Query = query,
                    Parameters = args
                }));

            return new GraphQueryResult
            {
                Keys = response.Columns,
                Values = response.Data,
                Result = JsonSerializer.Serialize(response.Data)
            };
        }
        catch (ApiException ex)
        {
            _logger.LogError($"Error when executing query in {Provider} graph db:\r\n{ex.Content}\r\n{query}\r\n{argLogs}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when executing query in {Provider} graph db. (Query: {query}), (Argments: \r\n{argLogs})");
            throw;
        }
    }
}
