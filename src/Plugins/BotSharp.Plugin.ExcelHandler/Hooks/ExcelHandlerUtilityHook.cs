namespace BotSharp.Plugin.ExcelHandler.Hooks;

public class ExcelHandlerUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(UtilityName.ExcelHandler);
    }
}