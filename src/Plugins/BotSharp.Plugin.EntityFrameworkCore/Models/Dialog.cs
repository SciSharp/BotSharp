namespace BotSharp.Plugin.EntityFrameworkCore.Entities;

public class Dialog
{
    public DialogMetaData MetaData { get; set; }
    public string Content { get; set; }
    public string? SecondaryContent { get; set; }
    public string? RichContent { get; set; }
    public string? SecondaryRichContent { get; set; }
    public string? Payload { get; set; }
}
