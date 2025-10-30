namespace BotSharp.Abstraction.Coding.Settings;

public class CodingSettings
{
    /// <summary>
    /// Llm provider to generate code script
    /// </summary>
    public string? Provider { get; set; }

    /// <summary>
    /// Llm model to generate code script
    /// </summary>
    public string? Model { get; set; }
}
