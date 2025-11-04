using BotSharp.Abstraction.Chart.Models;
using BotSharp.Abstraction.Chart.Options;

namespace BotSharp.Abstraction.Chart;

public interface IChartProcessor
{
    public string Provider { get; }

    Task<ChartDataResult?> GetConversationChartDataAsync(string conversationId, string messageId, ChartDataOptions? options = null)
        => throw new NotImplementedException();
}
