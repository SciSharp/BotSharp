using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.ChartHandler.Settings;

public class ChartHandlerSettings
{
    public ChartPlotSetting ChartPlot { get; set; }
}

public class ChartPlotSetting : LlmConfigBase
{
    public int? MessageLimit { get; set; }
}
