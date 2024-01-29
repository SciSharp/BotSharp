namespace BotSharp.Abstraction.Plugins;

public class PluginDependencyAttribute : Attribute
{
    public string[] PluginNames { get; set; }

    public PluginDependencyAttribute(params string[] pluginNames)
    {
        PluginNames = pluginNames;
    }
}
