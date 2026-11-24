using System.Net;
using System.Threading;
using BotSharp.Plugin.Membase.Models.Graph;
using Polly;
using Refit;

namespace BotSharp.Plugin.Membase.GraphDb;

public partial class MembaseGraphDb : IGraphDb
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MembaseGraphDb> _logger;
    private readonly IMembaseApi _membaseApi;

    private const int RETRY_COUNT = 3;
    // Membase reports Cypher compile/runtime failures as HTTP 500 problem details with this title.
    // Those failures are deterministic, so retrying them only delays the error.
    private const string QUERY_EXECUTION_ERROR_TITLE = "Query Execution Error";

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

    public async Task<GraphQueryResult> ExecuteQueryAsync(string query, GraphQueryExecuteOptions? options = null)
    {
        if (string.IsNullOrEmpty(options?.GraphId))
        {
            throw new ArgumentException($"Please provide a valid {Provider} graph id.");
        }

        var args = options?.Arguments ?? new();
        var argLogs = JsonSerializer.Serialize(args, BotSharpOptions.defaultJsonOptions);
        var callerToken = options!.CancellationToken;

        try
        {
            var retryPolicy = BuildRetryPolicy(options.Timeout);
            // The token must reach the HTTP call: Polly timeouts are optimistic and only fire through it.
            var response = await retryPolicy.ExecuteAsync(ct =>
                _membaseApi.CypherQueryAsync(options.GraphId, new CypherQueryRequest
                {
                    Query = query,
                    Parameters = args
                }, ct), callerToken);

            return new GraphQueryResult
            {
                Keys = response.Columns,
                Values = response.Data,
                Result = JsonSerializer.Serialize(response.Data)
            };
        }
        // The caller stopped waiting, which is not a Membase error, so it is not logged as one.
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
        {
            _logger.LogInformation("Query in {Provider} graph db was cancelled by its caller.", Provider);
            throw;
        }
        catch (Exception ex) when (callerToken.IsCancellationRequested)
        {
            // The HTTP client may wrap the cancellation in its own exception; surface it the standard way.
            _logger.LogInformation("Query in {Provider} graph db was cancelled by its caller.", Provider);
            throw new OperationCanceledException("The caller cancelled the query.", ex, callerToken);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, $"Error when executing query in {Provider} graph db:\r\n{ex.Content}\r\n{query}\r\n{argLogs}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when executing query in {Provider} graph db. (Query: {query}), (Argments: \r\n{argLogs})");
            throw;
        }
    }


    #region Node
    public async Task<GraphNodeModel> GetNodeAsync(string graphId, string nodeId)
    {
        var node = await _membaseApi.GetNodeAsync(graphId, nodeId);
        return Node.ToGraphNodeModel(node);
    }

    public async Task<GraphNodeModel> CreateNodeAsync(string graphId, GraphNodeCreationRequest request)
    {
        var node = await _membaseApi.CreateNodeAsync(graphId, new NodeCreationModel
        {
            Id = request.Id,
            Labels = request.Labels,
            Properties = request.Properties,
            Time = request.Time
        });
        return Node.ToGraphNodeModel(node);
    }

    public async Task<GraphNodeModel> MergeNodeAsync(string graphId, string nodeId, GraphNodeUpdateRequest request)
    {
        var node = await _membaseApi.MergeNodeAsync(graphId, nodeId, new NodeUpdateModel
        {
            Id = request.Id,
            Labels = request.Labels,
            Properties = request.Properties,
            Time = request.Time
        });
        return Node.ToGraphNodeModel(node);
    }

    public async Task<bool> DeleteNodeAsync(string graphId, string nodeId)
    {
        await _membaseApi.DeleteNodeAsync(graphId, nodeId);
        return true;
    }
    #endregion

    #region Edge
    public async Task<GraphEdgeModel> GetEdgeAsync(string graphId, string edgeId)
    {
        var edge = await _membaseApi.GetEdgeAsync(graphId, edgeId);
        return Edge.ToGraphEdgeModel(edge);
    }

    public async Task<GraphEdgeModel> CreateEdgeAsync(string graphId, GraphEdgeCreationRequest request)
    {
        var edge = await _membaseApi.CreateEdgeAsync(graphId, new EdgeCreationModel
        {
            Id = request.Id,
            SourceNodeId = request.SourceNodeId,
            TargetNodeId = request.TargetNodeId,
            Type = request.Type,
            Directed = request.Directed,
            Weight = request.Weight,
            Properties = request.Properties
        });
        return Edge.ToGraphEdgeModel(edge);
    }

    public async Task<GraphEdgeModel> UpdateEdgeAsync(string graphId, string edgeId, GraphEdgeUpdateRequest request)
    {
        var edge = await _membaseApi.UpdateEdgeAsync(graphId, edgeId, new EdgeUpdateModel
        {
            Id = request.Id,
            Properties = request.Properties
        });
        return Edge.ToGraphEdgeModel(edge);
    }

    public async Task<bool> DeleteEdgeAsync(string graphId, string edgeId)
    {
        await _membaseApi.DeleteEdgeAsync(graphId, edgeId);
        return true;
    }
    #endregion

    #region Private methods
    private AsyncPolicy BuildRetryPolicy(TimeSpan? totalTimeout)
    {
        // Timeouts are not retried: a query that ran out of time is usually just slow, and would run out again.
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<ApiException>(IsTransientApiException)
            .WaitAndRetryAsync(
                retryCount: RETRY_COUNT,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (ex, timespan, retryAttempt, _) =>
                {
                    _logger.LogWarning(ex,
                        "CypherQueryAsync retry {RetryAttempt}/{MaxRetries} after {Delay}s. Exception: {Message}",
                        retryAttempt, RETRY_COUNT, timespan.TotalSeconds, ex.Message);
                });

        var settings = _services.GetRequiredService<MembaseSettings>();
        var budget = totalTimeout is { } requested && requested > TimeSpan.Zero
            ? requested
            : TimeSpan.FromSeconds(settings.TimeoutSecond);
        if (budget <= TimeSpan.Zero)
        {
            return retryPolicy;
        }

        // One timeout around the whole query, back-off included, rather than one per attempt.
        return Policy.TimeoutAsync(budget).WrapAsync(retryPolicy);
    }

    private static bool IsTransientApiException(ApiException ex)
    {
        switch (ex.StatusCode)
        {
            case HttpStatusCode.BadGateway:
            case HttpStatusCode.ServiceUnavailable:
            case HttpStatusCode.GatewayTimeout:
                return true;
            case HttpStatusCode.InternalServerError:
                return !IsQueryExecutionError(ex.Content);
            default:
                return false;
        }
    }

    private static bool IsQueryExecutionError(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(content);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("title", out var title)
                && title.ValueKind == JsonValueKind.String
                && string.Equals(title.GetString(), QUERY_EXECUTION_ERROR_TITLE, StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }
    #endregion
}
