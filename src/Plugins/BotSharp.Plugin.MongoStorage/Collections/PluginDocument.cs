namespace BotSharp.Plugin.MongoStorage.Collections;

public class PluginDocument : MongoBase
{
    public List<string> EnabledPlugins { get; set; }
}
