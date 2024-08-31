namespace BotSharp.Abstraction.Infrastructures;

public class SharpCacheSettings
{
    public bool Enabled { get; set; } = false;
    public string Prefix { get; set; } = "cache";
}
