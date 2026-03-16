namespace BotSharp.Plugin.Membase.Settings;

public class MembaseSettings
{
    public string Host { get; set; } = "localhost";
    public string ProjectId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public GraphInstance[] GraphInstances { get; set; } = [];
}

public class GraphInstance
{
    /// <summary>
    /// Graph id
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Graph name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Graph description
    /// </summary>
    public string Description { get; set; }
}