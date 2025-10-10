namespace BotSharp.Plugin.FileHandler.Hooks;

public class FileHandlerUtilityHook : IAgentUtilityHook
{
    private const string READ_PDF_FN = "util-file-read_pdf";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var items = new List<AgentUtility>
        {
            new AgentUtility
            {
                Category = "file",
                Name = UtilityName.PdfReader,
                Items = [
                    new UtilityItem
                    {
                        FunctionName = READ_PDF_FN,
                        TemplateName = $"{READ_PDF_FN}.fn"
                    }
                ]
            }
        };

        utilities.AddRange(items);
    }
}
