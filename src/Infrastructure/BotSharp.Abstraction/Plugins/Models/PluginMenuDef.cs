namespace BotSharp.Abstraction.Plugins.Models;

public class PluginMenuDef
{
    public string Label { get; set; }
    public string? Icon { get; set; }
    public string? Link { get; set; }
    public bool IsHeader {  get; set; }
    public int Weight { get; set; }

    public PluginMenuDef(string lable, string? link = null, string? icon = null, int weight = 0, bool isHeader = false) 
    { 
        Label = lable;
        Link = link;
        Icon = icon;
        Weight = weight;
        IsHeader = isHeader;
    }
}
