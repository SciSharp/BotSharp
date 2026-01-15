using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace BotSharp.Plugin.AgentSkills.Loading;

/// <summary>
/// Parses SKILL.md files to extract YAML frontmatter and create <see cref="SkillMetadata"/>.
/// </summary>
public sealed class SkillParser
{
    private const string FrontmatterDelimiter = "---";

    private readonly IDeserializer _yamlDeserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkillParser"/> class.
    /// </summary>
    public SkillParser()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Parses a SKILL.md file and extracts the skill metadata.
    /// </summary>
    /// <param name="skillFilePath">The path to the SKILL.md file.</param>
    /// <param name="source">The source location of the skill.</param>
    /// <returns>The parsed skill metadata.</returns>
    /// <exception cref="SkillParseException">Thrown when parsing fails.</exception>
    public SkillMetadata Parse(string skillFilePath, SkillSource source)
    {
        // Validate the file exists and is within size limits
        var fileValidation = SkillValidator.ValidateSkillFile(skillFilePath);
        if (!fileValidation.IsValid)
        {
            throw new SkillParseException(skillFilePath, fileValidation.ErrorMessage!);
        }

        var content = File.ReadAllText(skillFilePath);
        var skillDirectory = Path.GetDirectoryName(skillFilePath)!;
        var directoryName = Path.GetFileName(skillDirectory);

        return ParseContent(content, skillDirectory, directoryName, source);
    }

    /// <summary>
    /// Parses SKILL.md content and extracts the skill metadata.
    /// </summary>
    /// <param name="content">The content of the SKILL.md file.</param>
    /// <param name="skillDirectory">The directory containing the skill.</param>
    /// <param name="directoryName">The name of the skill directory.</param>
    /// <param name="source">The source location of the skill.</param>
    /// <returns>The parsed skill metadata.</returns>
    /// <exception cref="SkillParseException">Thrown when parsing fails.</exception>
    public SkillMetadata ParseContent(string content, string skillDirectory, string directoryName, SkillSource source)
    {
        var frontmatter = ExtractFrontmatter(content);
        if (frontmatter is null)
        {
            throw new SkillParseException(skillDirectory, "SKILL.md must have YAML frontmatter delimited by '---'.");
        }

        SkillFrontmatter yamlData;
        try
        {
            yamlData = _yamlDeserializer.Deserialize<SkillFrontmatter>(frontmatter);
        }
        catch (Exception ex)
        {
            throw new SkillParseException(skillDirectory, $"Failed to parse YAML frontmatter: {ex.Message}", ex);
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(yamlData.Name))
        {
            throw new SkillParseException(skillDirectory, "Skill 'name' is required in frontmatter.");
        }

        if (string.IsNullOrWhiteSpace(yamlData.Description))
        {
            throw new SkillParseException(skillDirectory, "Skill 'description' is required in frontmatter.");
        }

        // Validate name format
        var nameValidation = SkillValidator.ValidateName(yamlData.Name);
        if (!nameValidation.IsValid)
        {
            throw new SkillParseException(skillDirectory, nameValidation.ErrorMessage!);
        }

        // Validate name matches directory
        var matchValidation = SkillValidator.ValidateNameMatchesDirectory(yamlData.Name, directoryName);
        if (!matchValidation.IsValid)
        {
            throw new SkillParseException(skillDirectory, matchValidation.ErrorMessage!);
        }

        // Validate description length
        var descValidation = SkillValidator.ValidateDescription(yamlData.Description);
        if (!descValidation.IsValid)
        {
            throw new SkillParseException(skillDirectory, descValidation.ErrorMessage!);
        }

        // Parse allowed tools
        var allowedTools = AllowedTool.Parse(yamlData.AllowedTools);

        // Build metadata dictionary
        Dictionary<string, string>? metadata = null;
        if (yamlData.Metadata is not null && yamlData.Metadata.Count > 0)
        {
            metadata = new Dictionary<string, string>(yamlData.Metadata);
        }

        return new SkillMetadata
        {
            Name = yamlData.Name,
            Description = yamlData.Description,
            Path = skillDirectory,
            Source = source,
            License = yamlData.License,
            Compatibility = yamlData.Compatibility,
            Metadata = metadata,
            AllowedTools = allowedTools.Count > 0 ? allowedTools : null
        };
    }

    /// <summary>
    /// Extracts the YAML frontmatter from SKILL.md content.
    /// </summary>
    private static string? ExtractFrontmatter(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var lines = content.Split('\n');

        // First line must be the frontmatter delimiter
        if (lines.Length == 0 || lines[0].Trim() != FrontmatterDelimiter)
        {
            return null;
        }

        // Find the closing delimiter
        var frontmatterLines = new List<string>();
        for (var i = 1; i < lines.Length; i++)
        {
            if (lines[i].Trim() == FrontmatterDelimiter)
            {
                return string.Join('\n', frontmatterLines);
            }
            frontmatterLines.Add(lines[i]);
        }

        // No closing delimiter found
        return null;
    }

    /// <summary>
    /// Internal class representing the YAML frontmatter structure.
    /// </summary>
    private sealed class SkillFrontmatter
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? License { get; set; }
        public string? Compatibility { get; set; }
        public string? AllowedTools { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }
}

/// <summary>
/// Exception thrown when skill parsing fails.
/// </summary>
public sealed class SkillParseException : Exception
{
    /// <summary>
    /// Gets the path to the skill that failed to parse.
    /// </summary>
    public string SkillPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkillParseException"/> class.
    /// </summary>
    public SkillParseException(string skillPath, string message)
        : base($"Failed to parse skill at '{skillPath}': {message}")
    {
        SkillPath = skillPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkillParseException"/> class.
    /// </summary>
    public SkillParseException(string skillPath, string message, Exception innerException)
        : base($"Failed to parse skill at '{skillPath}': {message}", innerException)
    {
        SkillPath = skillPath;
    }
}
