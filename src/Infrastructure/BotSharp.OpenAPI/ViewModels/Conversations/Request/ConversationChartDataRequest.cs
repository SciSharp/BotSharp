using BotSharp.Abstraction.Chart.Options;

namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class ConversationChartDataRequest : ChartDataOptions
{
    /// <summary>
    /// Chart service provider
    /// </summary>
    public string ChartProvider { get; set; } = "Botsharp";
}

public class ConversationChartCodeRequest : ChartCodeOptions
{
    /// <summary>
    /// Chart service provider
    /// </summary>
    public string ChartProvider { get; set; } = "Botsharp";
}
