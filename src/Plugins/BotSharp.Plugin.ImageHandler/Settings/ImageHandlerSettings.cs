using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.ImageHandler.Settings;

public class ImageHandlerSettings
{
    public ImageReadSettings? Reading { get; set; }
    public ImageCompositionSettings? Composition { get; set; }
}

public class ImageReadSettings : LlmProviderModel
{
    public string? ImageDetailLevel { get; set; }
}

public class ImageCompositionSettings : LlmProviderModel
{
    public SettingBase? ImageConverter { get; set; }
}
