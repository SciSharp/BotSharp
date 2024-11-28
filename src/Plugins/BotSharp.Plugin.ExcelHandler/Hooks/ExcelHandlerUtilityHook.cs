namespace BotSharp.Plugin.ExcelHandler.Hooks;

public class ExcelHandlerUtilityHook : IAgentUtilityHook
{
    private const string HANDLER_EXCEL = "handle_excel_request";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Name = UtilityName.ExcelHandler,
            Functions = [new(HANDLER_EXCEL)],
            Templates = [new($"{HANDLER_EXCEL}.fn")]
        };

        utilities.Add(utility);
    }
}