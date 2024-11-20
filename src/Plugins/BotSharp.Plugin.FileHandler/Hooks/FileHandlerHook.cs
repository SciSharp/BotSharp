namespace BotSharp.Plugin.FileHandler.Hooks;

public class FileHandlerHook : AgentHookBase, IAgentHook
{
    private const string READ_IMAGE_FN = "read_image";
    private const string READ_PDF_FN = "read_pdf";
    private const string GENERATE_IMAGE_FN = "generate_image";
    private const string EDIT_IMAGE_FN = "edit_image";

    public override string SelfId => string.Empty;

    public FileHandlerHook(IServiceProvider services, AgentSettings settings) : base(services, settings)
    {
    }

    public override void OnAgentLoaded(Agent agent)
    {
        var utilityLoads = new List<AgentUtilityLoadModel>
        {
            new AgentUtilityLoadModel
            {
                UtilityName = UtilityName.ImageGenerator,
                Content = new UtilityContent
                {
                    Functions = [new(GENERATE_IMAGE_FN)],
                    Templates = [new($"{GENERATE_IMAGE_FN}.fn")]
                }
            },
            new AgentUtilityLoadModel
            {
                UtilityName = UtilityName.ImageReader,
                Content = new UtilityContent
                {
                    Functions = [new(READ_IMAGE_FN)],
                    Templates = [new($"{READ_IMAGE_FN}.fn")]
                }
            },
            new AgentUtilityLoadModel
            {
                UtilityName = UtilityName.ImageEditor,
                Content = new UtilityContent
                {
                    Functions = [new(EDIT_IMAGE_FN)],
                    Templates = [new($"{EDIT_IMAGE_FN}.fn")]
                }
            },
            new AgentUtilityLoadModel
            {
                UtilityName = UtilityName.PdfReader,
                Content = new UtilityContent
                {
                    Functions = [new(READ_PDF_FN)],
                    Templates = [new($"{READ_PDF_FN}.fn")]
                }
            }
        };

        base.OnLoadAgentUtility(agent, utilityLoads);
        base.OnAgentLoaded(agent);
    }
}
