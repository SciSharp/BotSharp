namespace BotSharp.Plugin.AgentSkills.Settings;

/// <summary>
/// Configuration settings for the Agent Skills plugin.
/// </summary>
public class AgentSkillsSettings
{
    /// <summary>
    /// Enable user-level skills from ~/.botsharp/skills/
    /// </summary>
    public bool EnableUserSkills { get; set; } = true;

    /// <summary>
    /// Enable project-level skills from {project}/.botsharp/skills/
    /// </summary>
    public bool EnableProjectSkills { get; set; } = true;

    /// <summary>
    /// Override path for user skills directory. If null, uses default ~/.botsharp/skills/
    /// </summary>
    public string? UserSkillsDir { get; set; }

    /// <summary>
    /// Override path for project skills directory. If null, uses default {project}/.botsharp/skills/
    /// </summary>
    public string? ProjectSkillsDir { get; set; }

    /// <summary>
    /// Cache loaded skills in memory.
    /// </summary>
    public bool CacheSkills { get; set; } = true;

    /// <summary>
    /// Validate skills on startup.
    /// </summary>
    public bool ValidateOnStartup { get; set; } = true;

    /// <summary>
    /// Skills cache duration in seconds.
    /// </summary>
    public int SkillsCacheDurationSeconds { get; set; } = 300;

    /// <summary>
    /// Enable read_skill tool to read full SKILL.md content.
    /// </summary>
    public bool EnableReadSkillTool { get; set; } = true;

    /// <summary>
    /// Enable read_skill_file tool to read files in skill directories.
    /// </summary>
    public bool EnableReadFileTool { get; set; } = true;

    /// <summary>
    /// Enable list_skill_directory tool to list skill directory contents.
    /// </summary>
    public bool EnableListDirectoryTool { get; set; } = true;

    /// <summary>
    /// Maximum output size in bytes for skill content.
    /// </summary>
    public int MaxOutputSizeBytes { get; set; } = 50 * 1024;

    /// <summary>
    /// Gets the resolved user skills directory path.
    /// </summary>
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
    /// </summary>
    /// <param name="projectRoot">The project root directory.</param>
    public string? GetProjectSkillsDirectory(string? projectRoot = null)
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
    public string GetUserSkillPath(string skillName)
    {
        return Path.Combine(GetUserSkillsDirectory(), skillName);
    }

    /// <summary>
    /// Gets the path to a specific skill in project skills directory.
    /// </summary>
    public string? GetProjectSkillPath(string skillName, string? projectRoot = null)
    {
        var projectSkillsDir = GetProjectSkillsDirectory(projectRoot);
        return projectSkillsDir != null ? Path.Combine(projectSkillsDir, skillName) : null;
    }
}
