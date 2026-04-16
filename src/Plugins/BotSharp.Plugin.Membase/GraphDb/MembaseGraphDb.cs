using BotSharp.Plugin.Membase.Models.Graph;
using Polly;
using Polly.Timeout;
using Refit;

namespace BotSharp.Plugin.Membase.GraphDb;

public partial class MembaseGraphDb : IGraphDb
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MembaseGraphDb> _logger;
    private readonly IMembaseApi _membaseApi;

    private const int RETRY_COUNT = 3;

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
    private AsyncPolicy BuildRetryPolicy()
    {
        var settings = _services.GetRequiredService<MembaseSettings>();
        var timeoutSeconds = (double)settings.TimeoutSecond / RETRY_COUNT;

        var timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(timeoutSeconds));

        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutRejectedException>()
            .Or<ApiException>(ex => ex.StatusCode == HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(
                retryCount: RETRY_COUNT,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (ex, timespan, retryAttempt, _) =>
                {
                    _logger.LogWarning(ex,
                        "CypherQueryAsync retry {RetryAttempt}/{MaxRetries} after {Delay}s. Exception: {Message}",
                        retryAttempt, RETRY_COUNT, timespan.TotalSeconds, ex.Message);
                });

        return Policy.WrapAsync(retryPolicy, timeoutPolicy);
    }
    #endregion
}
