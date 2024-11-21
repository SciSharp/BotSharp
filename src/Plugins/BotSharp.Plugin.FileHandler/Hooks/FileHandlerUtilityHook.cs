namespace BotSharp.Plugin.FileHandler.Hooks;

public class FileHandlerUtilityHook : IAgentUtilityHook
{
    private const string READ_IMAGE_FN = "read_image";
    private const string READ_PDF_FN = "read_pdf";
    private const string GENERATE_IMAGE_FN = "generate_image";
    private const string EDIT_IMAGE_FN = "edit_image";

    public void AddUtilities(List<AgentUtility> utilities)
    {

        var items = new List<AgentUtility>
        {
            new AgentUtility
            {
                Name = UtilityName.ImageGenerator,
                Functions = [new(GENERATE_IMAGE_FN)],
                Templates = [new($"{GENERATE_IMAGE_FN}.fn")]
            },
            new AgentUtility
            {
                Name = UtilityName.ImageReader,
                Functions = [new(READ_IMAGE_FN)],
                Templates = [new($"{READ_IMAGE_FN}.fn")]
            },
            new AgentUtility
            {
                Name = UtilityName.ImageEditor,
                Functions = [new(EDIT_IMAGE_FN)],
                Templates = [new($"{EDIT_IMAGE_FN}.fn")]
            },
            new AgentUtility
            {
                Name = UtilityName.PdfReader,
                Functions = [new(READ_PDF_FN)],
                Templates = [new($"{READ_PDF_FN}.fn")]
            }
        };

        utilities.AddRange(items);
    }
}
