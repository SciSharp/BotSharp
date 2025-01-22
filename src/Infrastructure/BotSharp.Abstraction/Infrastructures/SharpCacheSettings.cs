namespace BotSharp.Abstraction.Infrastructures;

public class SharpCacheSettings
{
    public bool Enabled { get; set; } = true;
    public CacheType CacheType { get; set; } = Enums.CacheType.MemoryCache;
    public string Prefix { get; set; } = "cache";    
}
