namespace BotSharp.Plugin.ChartHandler.Hooks;

public class ChartHandlerUtilityHook : IAgentUtilityHook
{
    private const string GENERATE_CHART_FN = "util-chart-generate_chart";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var item = new AgentUtility
        {
            Category = "chart",
            Name = UtilityName.ChartGenerator,
            Items = [
                    new UtilityItem
                    {
                        FunctionName = GENERATE_CHART_FN,
                        TemplateName = $"{GENERATE_CHART_FN}.fn"
                    }
                ]
        };

        utilities.Add(item);
    }
}
