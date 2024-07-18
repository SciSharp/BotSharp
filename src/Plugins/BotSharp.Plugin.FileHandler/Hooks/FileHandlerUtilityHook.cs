namespace BotSharp.Plugin.FileHandler.Hooks;

public class FileHandlerUtilityHook : IAgentUtilityHook
{
    public void AddUtilities(List<string> utilities)
    {
        utilities.Add(UtilityName.ImageReader);
        utilities.Add(UtilityName.PdfReader);
        utilities.Add(UtilityName.ImageGenerator);
    }
}
