namespace BotSharp.Plugin.AgentSkills.Models;

/// <summary>
/// Represents an entry in a skill directory listing.
/// </summary>
public sealed record SkillDirectoryEntry
{
    /// <summary>
    /// The name of the file or directory.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Whether this is a directory.
    /// </summary>
    public bool IsDirectory { get; init; }

    /// <summary>
    /// The file size in bytes (only for files).
    /// </summary>
    public long? Size { get; init; }
}
