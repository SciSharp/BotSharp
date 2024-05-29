namespace BotSharp.Abstraction.Plugins.Models;

public class PluginMenuDef
{
    [JsonIgnore]
    public string Id { get; set; }

    public string Label { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsHeader {  get; set; }

    [JsonIgnore]
    public int Weight { get; set; }

    [JsonIgnore]
    public List<string>? Roles { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PluginMenuDef>? SubMenu {  get; set; }

    public PluginMenuDef(string lable, string? link = null, string? icon = null, int weight = 0) 
    { 
        Label = lable;
        Link = link;
        Icon = icon;
        Weight = weight;
    }

    public override string ToString()
    {
        return $"{Label} {Link} {Weight}";
    }
}
