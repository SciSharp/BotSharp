using BotSharp.Abstraction.Models;

namespace BotSharp.Plugin.FileHandler.Settings;

public class FileHandlerSettings
{
    public PdfSettings? Pdf { get; set; }
}

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