using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.ImageHandler.Settings;

public class ImageHandlerSettings
{
    public ImageReadSettings? Reading { get; set; }
    public ImageGenerationSettings? Generation { get; set; }
    public ImageEditSettings? Edit { get; set; }
}

public class ImageReadSettings : LlmProviderModel
{
    public string? ImageDetailLevel { get; set; }
}

public class ImageGenerationSettings : LlmProviderModel
{

}

public class ImageEditSettings : LlmProviderModel
{
    public SettingBase? ImageConverter { get; set; }
}