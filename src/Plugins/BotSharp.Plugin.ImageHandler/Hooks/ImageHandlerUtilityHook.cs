namespace BotSharp.Plugin.ImageHandler.Hooks;

public class ImageHandlerUtilityHook : IAgentUtilityHook
{
    private const string READ_IMAGE_FN = "util-file-read_image";
    private const string GENERATE_IMAGE_FN = "util-file-generate_image";
    private const string EDIT_IMAGE_FN = "util-file-edit_image";
    private const string COMPOSE_IMAGES_FN = "util-file-edit_images";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var items = new List<AgentUtility>
        {
            new AgentUtility
            {
                Category = "file",
                Name = UtilityName.ImageReader,
                Items = [
                    new UtilityItem
                    {
                        FunctionName = READ_IMAGE_FN,
                        TemplateName = $"{READ_IMAGE_FN}.fn"
                    }
                ]
            },
            new AgentUtility
            {
                Category = "file",
                Name = UtilityName.ImageGenerator,
                Items = [
                    new UtilityItem
                    {
                        FunctionName = GENERATE_IMAGE_FN,
                        TemplateName = $"{GENERATE_IMAGE_FN}.fn"
                    }    
                ]
            },
            new AgentUtility
            {
                Category = "file",
                Name = UtilityName.ImageEditor,
                Items = [
                    new UtilityItem
                    {
                        FunctionName = EDIT_IMAGE_FN,
                        TemplateName = $"{EDIT_IMAGE_FN}.fn"
                    }
                ]
            },
            new AgentUtility
            {
                Category = "file",
                Name = UtilityName.ImageComposer,
                Items = [
                    new UtilityItem
                    {
                        FunctionName = COMPOSE_IMAGES_FN,
                        TemplateName = $"{COMPOSE_IMAGES_FN}.fn"
                    }
                ]
            },
        };

        utilities.AddRange(items);
    }
}
