namespace BotSharp.Plugin.ChartHandler.Settings;

public class ChartHandlerSettings
{
    public ChartPlotSetting ChartPlot { get; set; }
}

public class ChartPlotSetting
{
    public string LlmProvider { get; set; }
    public string LlmModel { get; set; }
}
