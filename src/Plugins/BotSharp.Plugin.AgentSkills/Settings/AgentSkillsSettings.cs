namespace BotSharp.Plugin.AgentSkills.Settings;

/// <summary>
/// Configuration settings for the Agent Skills plugin.
/// Implements requirements: FR-6.1, FR-6.2
/// </summary>
public class AgentSkillsSettings
{
    /// <summary>
    /// Enable user-level skills from ~/.botsharp/skills/
    /// Implements requirement: FR-1.2
    /// </summary>
    public bool EnableUserSkills { get; set; } = true;

    /// <summary>
    /// Enable project-level skills from {project}/.botsharp/skills/
    /// Implements requirement: FR-1.2
    /// </summary>
    public bool EnableProjectSkills { get; set; } = true;

    /// <summary>
    /// Override path for user skills directory. If null, uses default ~/.botsharp/skills/
    /// Implements requirement: FR-1.2
    /// </summary>
    public string? UserSkillsDir { get; set; }

    /// <summary>
    /// Override path for project skills directory. If null, uses default {project}/.botsharp/skills/
    /// Implements requirement: FR-1.2
    /// </summary>
    public string? ProjectSkillsDir { get; set; }

    /// <summary>
    /// Cache loaded skills in memory.
    /// Implements requirement: NFR-1.3
    /// </summary>
    public bool CacheSkills { get; set; } = true;

    /// <summary>
    /// Validate skills on startup.
    /// Implements requirement: FR-6.2
    /// </summary>
    public bool ValidateOnStartup { get; set; } = false;

    /// <summary>
    /// Skills cache duration in seconds. 0 means permanent cache.
    /// Implements requirement: NFR-1.3
    /// </summary>
    public int SkillsCacheDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Enable read_skill tool to read full SKILL.md content.
    /// Implements requirement: FR-3.2
    /// </summary>
    public bool EnableReadSkillTool { get; set; } = true;

    /// <summary>
    /// Enable read_skill_file tool to read files in skill directories.
    /// Implements requirement: FR-3.2
    /// </summary>
    public bool EnableReadFileTool { get; set; } = true;

    /// <summary>
    /// Enable list_skill_directory tool to list skill directory contents.
    /// Implements requirement: FR-3.2
    /// </summary>
    public bool EnableListDirectoryTool { get; set; } = true;

    /// <summary>
    /// Maximum output size in bytes for skill content (default: 50KB).
    /// Implements requirement: FR-5.2
    /// </summary>
    public int MaxOutputSizeBytes { get; set; } = 50 * 1024;

    /// <summary>
    /// Gets the resolved user skills directory path.
    /// Implements requirement: FR-1.2
    /// </summary>
    /// <returns>The absolute path to the user skills directory.</returns>
    public string GetUserSkillsDirectory()
    {
        if (!string.IsNullOrEmpty(UserSkillsDir))
        {
            return UserSkillsDir;
        }

        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(homeDir, ".botsharp", "skills");
    }

    /// <summary>
    /// Gets the resolved project skills directory path.
    /// Implements requirement: FR-1.2
    /// </summary>
    /// <param name="projectRoot">The project root directory. If null, uses current directory.</param>
    /// <returns>The absolute path to the project skills directory.</returns>
    public string GetProjectSkillsDirectory(string? projectRoot = null)
    {
        if (!string.IsNullOrEmpty(ProjectSkillsDir))
        {
            return ProjectSkillsDir;
        }

        if (string.IsNullOrEmpty(projectRoot))
        {
            projectRoot = Directory.GetCurrentDirectory();
        }

        return Path.Combine(projectRoot, ".botsharp", "skills");
    }

    /// <summary>
    /// Gets the path to a specific skill in user skills directory.
    /// </summary>
    /// <param name="skillName">The name of the skill.</param>
    /// <returns>The absolute path to the skill directory.</returns>
    public string GetUserSkillPath(string skillName)
    {
        return Path.Combine(GetUserSkillsDirectory(), skillName);
    }

    /// <summary>
    /// Gets the path to a specific skill in project skills directory.
    /// </summary>
    /// <param name="skillName">The name of the skill.</param>
    /// <param name="projectRoot">The project root directory. If null, uses current directory.</param>
    /// <returns>The absolute path to the skill directory.</returns>
    public string GetProjectSkillPath(string skillName, string? projectRoot = null)
    {
        var projectSkillsDir = GetProjectSkillsDirectory(projectRoot);
        return Path.Combine(projectSkillsDir, skillName);
    }

    /// <summary>
    /// Validates the configuration settings.
    /// Implements requirement: FR-6.2
    /// </summary>
    /// <returns>A collection of validation error messages. Empty if configuration is valid.</returns>
    public IEnumerable<string> Validate()
    {
        var errors = new List<string>();

        if (MaxOutputSizeBytes <= 0)
        {
            errors.Add("MaxOutputSizeBytes must be greater than 0");
        }

        if (SkillsCacheDurationSeconds < 0)
        {
            errors.Add("SkillsCacheDurationSeconds must be non-negative");
        }

        if (!EnableUserSkills && !EnableProjectSkills)
        {
            errors.Add("At least one of EnableUserSkills or EnableProjectSkills must be true");
        }

        return errors;
    }
}
