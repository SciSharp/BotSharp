namespace BotSharp.Abstraction.Settings;

public class ModelUpgradeMapSettings
{
    public static string Key => "ModelUpgradeMap";
    public List<ModelUpgradeMapItem> ModelUpgradeMap { get; set; } = new();
}

public class ModelUpgradeMapItem
{
    public string OldModel { get; set; } = string.Empty;
    public string NewModel { get; set; } = string.Empty;
    public bool Enable { get; set; }
}
