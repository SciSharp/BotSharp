namespace BotSharp.Plugin.AgentSkills.Models;

/// <summary>
/// Represents the metadata of a skill parsed from SKILL.md YAML frontmatter.
/// Follows the Agent Skills specification: https://agentskills.io
/// </summary>
public sealed record SkillMetadata
{
    /// <summary>
    /// Maximum allowed length for skill name.
    /// </summary>
    public const int MaxNameLength = 64;

    /// <summary>
    /// Maximum allowed length for skill description.
    /// </summary>
    public const int MaxDescriptionLength = 1024;

    /// <summary>
    /// Maximum file size for SKILL.md in bytes (10 MB).
    /// </summary>
    public const long MaxSkillFileSize = 10 * 1024 * 1024;

    /// <summary>
    /// The standard skill definition filename.
    /// </summary>
    public const string SkillFileName = "SKILL.md";

    /// <summary>
    /// Required. Skill identifier (lowercase alphanumeric with hyphens, max 64 characters).
    /// Must match the directory name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Required. Brief description of the skill's purpose (max 1024 characters).
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Absolute path to the skill directory containing SKILL.md.
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Whether the skill is from user-level or project-level location.
    /// </summary>
    public SkillSource Source { get; init; }

    /// <summary>
    /// Optional. SPDX license identifier (e.g., "MIT", "Apache-2.0").
    /// </summary>
    public string? License { get; init; }

    /// <summary>
    /// Optional. Compatibility constraints (e.g., "vscode", "cursor", "any").
    /// </summary>
    public string? Compatibility { get; init; }

    /// <summary>
    /// Optional. Additional key-value metadata pairs.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Optional. List of tools the skill is allowed to use.
    /// </summary>
    public IReadOnlyList<AllowedTool>? AllowedTools { get; init; }

    /// <summary>
    /// Gets the full path to the SKILL.md file.
    /// </summary>
    public string SkillFilePath => System.IO.Path.Combine(Path, SkillFileName);

    /// <summary>
    /// Returns a display string for the skill suitable for system prompts.
    /// </summary>
    public string ToDisplayString() => $"- **{Name}**: {Description}";
}
