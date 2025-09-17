using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.FileHandler.Settings;

public class FileHandlerSettings
{
    public ImageSettings? Image { get; set; }
    public PdfSettings? Pdf { get; set; }
}

#region Image
public class ImageSettings
{
    public ImageReadSettings? Reading { get; set; }
    public ImageGenerationSettings? Generation { get; set; }
    public ImageEditSettings? Edit { get; set; }
    public ImageVariationSettings? Variation { get; set; }
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

public class ImageVariationSettings : LlmBase
{

}
#endregion

#region Pdf
public class PdfSettings
{
    public PdfReadSettings? Reading { get; set; }
}

public class PdfReadSettings : LlmBase
{
    public bool ConvertToImage { get; set; }
    public string? ImageDetailLevel { get; set; }
    public SettingBase? ImageConverter { get; set; }
}
#endregion