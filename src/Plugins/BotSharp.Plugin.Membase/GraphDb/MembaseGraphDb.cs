using BotSharp.Abstraction.Graph;
using BotSharp.Abstraction.Graph.Models;
using BotSharp.Abstraction.Graph.Options;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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

    public async Task<GraphQueryResult> ExecuteQueryAsync(string query, GraphQueryExecuteOptions? options = null)
    {
        if (string.IsNullOrEmpty(options?.GraphId))
        {
            throw new ArgumentException($"Please provide a valid {Provider} graph id.");
        }

        try
        {
            var response = await _membaseApi.CypherQueryAsync(options.GraphId, new CypherQueryRequest
            {
                Query = query,
                Parameters = options.Arguments ?? new Dictionary<string, object>()
            });

            return new GraphQueryResult
            {
                Keys = response.Columns,
                Values = response.Data,
                Result = JsonSerializer.Serialize(response.Data)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when executing query in {Provider} graph db.");
            return new();
        }
    }
}
