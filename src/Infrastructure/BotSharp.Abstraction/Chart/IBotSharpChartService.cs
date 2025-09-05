using BotSharp.Abstraction.Chart.Models;

namespace BotSharp.Abstraction.Chart;

public interface IBotSharpChartService
{
    public string Provider { get; }

    Task<ChartDataResult?> GetConversationChartData(string conversationId, string messageId, ChartDataOptions options)
        => throw new NotImplementedException();

    Task<ChartCodeResult?> GetConversationChartCode(string conversationId, string messageId, ChartCodeOptions options)
        => throw new NotImplementedException();
}
