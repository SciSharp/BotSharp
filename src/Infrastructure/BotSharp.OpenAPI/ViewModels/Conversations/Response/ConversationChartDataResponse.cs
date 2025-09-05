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


public class ConversationChartCodeResponse
{
    public string Code { get; set; }
    public string Language { get; set; }

    public static ConversationChartCodeResponse? From(ChartCodeResult? result)
    {
        if (result == null)
        {
            return null;
        }

        return new()
        {
            Code = result.Code,
            Language = result.Language
        };
    }
}
