namespace BotSharp.Plugin.ExcelHandler.Hooks;

public class ExcelHandlerUtilityHook : IAgentUtilityHook
{
    private static string PREFIX = "util-excel-";
    private static string HANDLER_EXCEL = $"{PREFIX}handle_excel_request";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Category = "file",
            Name = UtilityName.ExcelHandler,
            Items = [
                new UtilityItem
                {
                    FunctionName = HANDLER_EXCEL,
                    TemplateName = $"{HANDLER_EXCEL}.fn"
                }
            ]
        };

        utilities.Add(utility);
    }
}