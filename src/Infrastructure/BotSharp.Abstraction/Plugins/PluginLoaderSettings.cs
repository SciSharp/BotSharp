namespace BotSharp.Abstraction.Plugins;

public class PluginSettings
{
    public string[] Assemblies { get; set; } = new string[0];

    public string[] ExcludedFunctions { get; set; } = new string[0];
}
