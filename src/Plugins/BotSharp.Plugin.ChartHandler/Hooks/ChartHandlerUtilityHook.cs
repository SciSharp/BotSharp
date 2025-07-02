namespace BotSharp.Plugin.ChartHandler.Hooks;

public class ChartHandlerUtilityHook : IAgentUtilityHook
{
    private const string PLOT_CHART_FN = "util-chart-plot_chart";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var item = new AgentUtility
        {
            Category = "chart",
            Name = UtilityName.ChartPlotter,
            Items = [
                new UtilityItem
                {
                    FunctionName = PLOT_CHART_FN,
                    TemplateName = $"{PLOT_CHART_FN}.fn"
                }
            ]
        };

        utilities.Add(item);
    }
}
