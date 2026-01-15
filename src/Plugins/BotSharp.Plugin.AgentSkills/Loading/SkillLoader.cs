using Microsoft.Extensions.Logging.Abstractions;

namespace BotSharp.Plugin.AgentSkills.Loading;

/// <summary>
/// Discovers, validates, and loads skills from configured directories.
/// </summary>
public sealed class SkillLoader
{
    private readonly SkillParser _parser;
    private readonly ILogger<SkillLoader> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkillLoader"/> class.
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    public SkillLoader(ILogger<SkillLoader>? logger = null)
    {
        _parser = new SkillParser();
        _logger = logger ?? NullLogger<SkillLoader>.Instance;
    }

    /// <summary>
    /// Loads all skills from the configured directories.
    /// </summary>
    /// <param name="settings">The skills settings containing directory paths.</param>
    /// <returns>The loaded skills state.</returns>
    public SkillsState LoadSkills(AgentSkillsSettings settings)
    {
        var userSkills = new List<SkillMetadata>();
        var projectSkills = new List<SkillMetadata>();

        // Load user-level skills
        if (settings.EnableUserSkills)
        {
            var userSkillsDir = settings.GetUserSkillsDirectory();
            userSkills.AddRange(LoadSkillsFromDirectory(userSkillsDir, SkillSource.User));
        }

        // Load project-level skills
        if (settings.EnableProjectSkills)
        {
            var projectSkillsDir = settings.GetProjectSkillsDirectory();
            if (projectSkillsDir is not null)
            {
                projectSkills.AddRange(LoadSkillsFromDirectory(projectSkillsDir, SkillSource.Project));
            }
        }

        _logger.LogInformation(
            "Loaded {UserCount} user skills and {ProjectCount} project skills",
            userSkills.Count,
            projectSkills.Count);

        return new SkillsState
        {
            UserSkills = userSkills,
            ProjectSkills = projectSkills,
            LastRefreshed = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Loads a single skill by name.
    /// </summary>
    /// <param name="skillName">The name of the skill to load.</param>
    /// <param name="settings">The skills settings containing directory paths.</param>
    /// <returns>The loaded skill metadata, or null if not found.</returns>
    public SkillMetadata? LoadSkill(string skillName, AgentSkillsSettings settings)
    {
        // Check project-level first (takes precedence)
        var projectSkillPath = settings.GetProjectSkillPath(skillName);
        if (projectSkillPath is not null)
        {
            var skill = TryLoadSkill(projectSkillPath, SkillSource.Project);
            if (skill is not null)
            {
                return skill;
            }
        }

        // Check user-level
        var userSkillPath = settings.GetUserSkillPath(skillName);
        return TryLoadSkill(userSkillPath, SkillSource.User);
    }

    /// <summary>
    /// Loads all skills from a specific directory.
    /// </summary>
    /// <param name="skillsDirectory">The directory containing skill subdirectories.</param>
    /// <param name="source">The source type for loaded skills.</param>
    /// <returns>Collection of successfully loaded skills.</returns>
    public IEnumerable<SkillMetadata> LoadSkillsFromDirectory(string skillsDirectory, SkillSource source)
    {
        if (!Directory.Exists(skillsDirectory))
        {
            _logger.LogDebug("Skills directory does not exist: {Directory}", skillsDirectory);
            yield break;
        }

        var skillDirectories = Directory.GetDirectories(skillsDirectory);

        foreach (var skillDir in skillDirectories)
        {
            var skill = TryLoadSkill(skillDir, source);
            if (skill is not null)
            {
                yield return skill;
            }
        }
    }

    /// <summary>
    /// Attempts to load a skill from a directory.
    /// </summary>
    /// <param name="skillDirectory">The skill directory path.</param>
    /// <param name="source">The source type for the skill.</param>
    /// <returns>The loaded skill metadata, or null if loading fails.</returns>
    private SkillMetadata? TryLoadSkill(string skillDirectory, SkillSource source)
    {
        var skillFilePath = Path.Combine(skillDirectory, SkillMetadata.SkillFileName);

        if (!File.Exists(skillFilePath))
        {
            _logger.LogDebug("No SKILL.md found in: {Directory}", skillDirectory);
            return null;
        }

        // Security check: ensure the skill file is not a symlink pointing outside
        if (IsSymbolicLink(skillFilePath))
        {
            var realPath = GetRealPath(skillFilePath);
            if (realPath is null || !IsPathSafe(realPath, skillDirectory))
            {
                _logger.LogWarning(
                    "Skipping skill with potentially unsafe symlink: {Directory}",
                    skillDirectory);
                return null;
            }
        }

        try
        {
            var skill = _parser.Parse(skillFilePath, source);
            _logger.LogDebug("Loaded skill: {SkillName} from {Source}", skill.Name, source);
            return skill;
        }
        catch (SkillParseException ex)
        {
            _logger.LogWarning("Failed to parse skill: {Error}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading skill from: {Directory}", skillDirectory);
            return null;
        }
    }

    /// <summary>
    /// Reads the full content of a SKILL.md file.
    /// </summary>
    /// <param name="skill">The skill metadata.</param>
    /// <returns>The full content of the SKILL.md file.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    public string ReadSkillContent(SkillMetadata skill)
    {
        var skillFilePath = skill.SkillFilePath;

        if (!File.Exists(skillFilePath))
        {
            throw new FileNotFoundException($"SKILL.md not found for skill '{skill.Name}'", skillFilePath);
        }

        return File.ReadAllText(skillFilePath);
    }

    /// <summary>
    /// Reads a file within a skill directory.
    /// </summary>
    /// <param name="skill">The skill metadata.</param>
    /// <param name="relativePath">Relative path to the file within the skill directory.</param>
    /// <returns>The file content.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if path traversal is detected.</exception>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    public string ReadSkillFile(SkillMetadata skill, string relativePath)
    {
        var safePath = ResolveSafePath(skill.Path, relativePath);
        if (safePath is null)
        {
            throw new UnauthorizedAccessException($"Path traversal attempt detected: {relativePath}");
        }

        if (!File.Exists(safePath))
        {
            throw new FileNotFoundException($"File not found: {relativePath}", safePath);
        }

        return File.ReadAllText(safePath);
    }

    /// <summary>
    /// Lists files in a skill directory.
    /// </summary>
    /// <param name="skill">The skill metadata.</param>
    /// <param name="relativePath">Optional relative path within the skill directory.</param>
    /// <returns>Collection of file and directory entries.</returns>
    public IEnumerable<SkillDirectoryEntry> ListSkillDirectory(SkillMetadata skill, string? relativePath = null)
    {
        var targetDir = skill.Path;

        if (!string.IsNullOrEmpty(relativePath))
        {
            var safePath = ResolveSafePath(skill.Path, relativePath);
            if (safePath is null)
            {
                throw new UnauthorizedAccessException($"Path traversal attempt detected: {relativePath}");
            }
            targetDir = safePath;
        }

        if (!Directory.Exists(targetDir))
        {
            yield break;
        }

        foreach (var dir in Directory.GetDirectories(targetDir))
        {
            var name = Path.GetFileName(dir);
            yield return new SkillDirectoryEntry { Name = name, IsDirectory = true };
        }

        foreach (var file in Directory.GetFiles(targetDir))
        {
            var name = Path.GetFileName(file);
            var size = new FileInfo(file).Length;
            yield return new SkillDirectoryEntry { Name = name, IsDirectory = false, Size = size };
        }
    }

    /// <summary>
    /// Checks if a path is a symbolic link.
    /// </summary>
    private static bool IsSymbolicLink(string path)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.LinkTarget != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the real path of a symbolic link.
    /// </summary>
    private static string? GetRealPath(string path)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            return fileInfo.LinkTarget != null ? Path.GetFullPath(fileInfo.LinkTarget) : Path.GetFullPath(path);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if a path is safe (within the base directory).
    /// </summary>
    private static bool IsPathSafe(string path, string baseDir)
    {
        var normalizedPath = Path.GetFullPath(path);
        var normalizedBase = Path.GetFullPath(baseDir);
        return normalizedPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves a relative path within a base directory safely.
    /// </summary>
    private static string? ResolveSafePath(string baseDir, string relativePath)
    {
        try
        {
            var combined = Path.Combine(baseDir, relativePath);
            var fullPath = Path.GetFullPath(combined);
            var normalizedBase = Path.GetFullPath(baseDir);

            if (!fullPath.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return fullPath;
        }
        catch
        {
            return null;
        }
    }
}
