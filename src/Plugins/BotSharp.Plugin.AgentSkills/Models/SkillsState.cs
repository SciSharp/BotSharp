namespace BotSharp.Plugin.AgentSkills.Models;

/// <summary>
/// Represents the current state of loaded skills.
/// </summary>
public sealed class SkillsState
{
    /// <summary>
    /// Gets or sets the collection of user-level skills.
    /// </summary>
    public IReadOnlyList<SkillMetadata> UserSkills { get; init; } = [];

    /// <summary>
    /// Gets or sets the collection of project-level skills.
    /// </summary>
    public IReadOnlyList<SkillMetadata> ProjectSkills { get; init; } = [];

    /// <summary>
    /// Gets the timestamp when skills were last loaded.
    /// </summary>
    public DateTimeOffset LastRefreshed { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets all skills combined, with project skills taking precedence over user skills
    /// when there are name conflicts.
    /// </summary>
    public IReadOnlyList<SkillMetadata> AllSkills
    {
        get
        {
            var projectSkillNames = ProjectSkills.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var userSkillsWithoutOverrides = UserSkills.Where(s => !projectSkillNames.Contains(s.Name));
            return [.. ProjectSkills, .. userSkillsWithoutOverrides];
        }
    }

    /// <summary>
    /// Gets a skill by name, checking project skills first, then user skills.
    /// </summary>
    /// <param name="name">The skill name to find.</param>
    /// <returns>The skill metadata if found; otherwise, null.</returns>
    public SkillMetadata? GetSkill(string name)
    {
        return ProjectSkills.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            ?? UserSkills.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets skills filtered by source.
    /// </summary>
    /// <param name="source">The source to filter by.</param>
    /// <returns>Skills from the specified source.</returns>
    public IReadOnlyList<SkillMetadata> GetSkillsBySource(SkillSource source)
    {
        return source switch
        {
            SkillSource.User => UserSkills,
            SkillSource.Project => ProjectSkills,
            _ => []
        };
    }
}
