using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.ImageHandler.Settings;

public class ImageHandlerSettings
{
    public ImageReadSettings? Reading { get; set; }
    public ImageGenerationSettings? Generation { get; set; }
    public ImageEditSettings? Edit { get; set; }
}

public class ImageReadSettings : LlmBase
{
    public string? ImageDetailLevel { get; set; }
}

public class ImageGenerationSettings : LlmBase
{

}

public class ImageEditSettings : LlmBase
{
    public SettingBase? ImageConverter { get; set; }
}