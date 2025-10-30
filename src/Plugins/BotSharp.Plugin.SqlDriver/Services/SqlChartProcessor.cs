using BotSharp.Abstraction.Chart.Options;
using BotSharp.Abstraction.Repositories;

namespace BotSharp.Plugin.SqlDriver.Services;

public class SqlChartProcessor : IChartProcessor
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SqlChartProcessor> _logger;

    public SqlChartProcessor(
        IServiceProvider services,
        ILogger<SqlChartProcessor> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Provider => "sql_driver";

    public async Task<ChartDataResult?> GetConversationChartDataAsync(string conversationId, string messageId, ChartDataOptions? options = null)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(options?.TargetStateName))
        {
            var db = _services.GetRequiredService<IBotSharpRepository>();
            var states = db.GetConversationStates(conversationId);
            var value = states?.GetValueOrDefault(options?.TargetStateName)?.Values?.LastOrDefault()?.Data;

            // To do
            //return new ChartDataResult();
        }

        // Dummy data for testing
        var data = new
        {
            categories = new string[] { "A", "B", "C", "D", "E" },
            values = new int[] { 42, 67, 29, 85, 53 }
        };

        return new ChartDataResult { Data = data };
    }
}
