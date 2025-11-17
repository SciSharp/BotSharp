namespace BotSharp.OpenAPI.ViewModels.Conversations;

public class ConversationChartDataResponse
{
    public object Data { get; set; }

    public static ConversationChartDataResponse? From(ChartDataResult? result)
    {
        if (result == null)
        {
            return null;
        }

        return new()
        {
            Data = result.Data
        };
    }
}