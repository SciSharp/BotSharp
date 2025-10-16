namespace BotSharp.Abstraction.Diagnostics;

public class BotSharpOTelOptions
{
    public const string DefaultName = "BotSharp.Server";

    public string Name { get; set; } = DefaultName;

    public string Version { get; set; } = "4.0.0";

    public bool IsTelemetryEnabled { get; set; } = true;
}
