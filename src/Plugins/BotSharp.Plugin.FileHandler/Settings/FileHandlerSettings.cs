namespace BotSharp.Plugin.FileHandler.Settings;

public class FileHandlerSettings
{
    public ImageSettings? Image { get; set; }
    public PdfSettings? Pdf { get; set; }
    public SettingBase? ImageConverter { get; set; }
}

#region Image
public class ImageSettings
{
    public ImageReadSettings? Reading { get; set; }
    public ImageGenerationSettings? Generation { get; set; }
    public ImageEditSettings? Edit { get; set; }
    public ImageVariationSettings? Variation { get; set; }
}

public class ImageReadSettings : FileLlmSettingBase
{
    public string? ImageDetailLevel { get; set; }
}

public class ImageGenerationSettings : FileLlmSettingBase
{

}

public class ImageEditSettings : FileLlmSettingBase
{

}

public class ImageVariationSettings : FileLlmSettingBase
{

}
#endregion

#region Pdf
public class PdfSettings
{
    public PdfReadSettings? Reading { get; set; }
}

public class PdfReadSettings : FileLlmSettingBase
{
    public bool ConvertToImage { get; set; }
    public string? ImageDetailLevel { get; set; }
}
#endregion

public class FileLlmSettingBase
{
    public string? LlmProvider { get; set; }
    public string? LlmModel { get; set; }
}