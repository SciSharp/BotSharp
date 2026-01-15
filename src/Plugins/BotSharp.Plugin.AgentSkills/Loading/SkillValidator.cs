namespace BotSharp.Plugin.AgentSkills.Loading;

/// <summary>
/// Validates skill names and files according to the Agent Skills specification.
/// </summary>
public static partial class SkillValidator
{
    /// <summary>
    /// Pattern for valid skill names: lowercase alphanumeric with hyphens, max 64 characters.
    /// </summary>
    private static readonly Regex SkillNamePatternRegex = new Regex(
        @"^[a-z0-9][a-z0-9\-]{0,62}[a-z0-9]$|^[a-z0-9]$",
        RegexOptions.Compiled);

    /// <summary>
    /// Validates a skill name according to the Agent Skills specification.
    /// </summary>
    /// <param name="name">The skill name to validate.</param>
    /// <returns>A validation result indicating success or failure with error message.</returns>
    public static SkillValidationResult ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return SkillValidationResult.Failure("Skill name cannot be null or empty.");
        }

        if (name.Length > 64)
        {
            return SkillValidationResult.Failure($"Skill name exceeds maximum length of 64 characters. Actual: {name.Length}");
        }

        if (!SkillNamePatternRegex.IsMatch(name))
        {
            return SkillValidationResult.Failure(
                "Skill name must contain only lowercase letters, numbers, and hyphens. " +
                "Must start and end with a letter or number.");
        }

        return SkillValidationResult.Success();
    }

    /// <summary>
    /// Validates that a skill name matches its directory name.
    /// </summary>
    /// <param name="skillName">The skill name from YAML frontmatter.</param>
    /// <param name="directoryName">The directory name containing the skill.</param>
    /// <returns>A validation result indicating success or failure with error message.</returns>
    public static SkillValidationResult ValidateNameMatchesDirectory(string skillName, string directoryName)
    {
        if (!string.Equals(skillName, directoryName, StringComparison.OrdinalIgnoreCase))
        {
            return SkillValidationResult.Failure(
                $"Skill name '{skillName}' does not match directory name '{directoryName}'.");
        }

        return SkillValidationResult.Success();
    }

    /// <summary>
    /// Validates a skill description.
    /// </summary>
    /// <param name="description">The description to validate.</param>
    /// <returns>A validation result indicating success or failure with error message.</returns>
    public static SkillValidationResult ValidateDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return SkillValidationResult.Failure("Skill description cannot be null or empty.");
        }

        if (description.Length > 1024)
        {
            return SkillValidationResult.Failure(
                $"Skill description exceeds maximum length of 1024 characters. Actual: {description.Length}");
        }

        return SkillValidationResult.Success();
    }

    /// <summary>
    /// Validates that a SKILL.md file exists and is within size limits.
    /// </summary>
    /// <param name="skillFilePath">The path to the SKILL.md file.</param>
    /// <returns>A validation result indicating success or failure with error message.</returns>
    public static SkillValidationResult ValidateSkillFile(string skillFilePath)
    {
        if (!File.Exists(skillFilePath))
        {
            return SkillValidationResult.Failure($"SKILL.md file not found at: {skillFilePath}");
        }

        var fileInfo = new FileInfo(skillFilePath);
        if (fileInfo.Length > 10 * 1024 * 1024) // 10 MB limit
        {
            return SkillValidationResult.Failure(
                $"SKILL.md file exceeds maximum size of 10 MB. Actual: {fileInfo.Length / (1024 * 1024):F2} MB");
        }

        return SkillValidationResult.Success();
    }
}

/// <summary>
/// Represents the result of a skill validation operation.
/// </summary>
public readonly struct SkillValidationResult
{
    /// <summary>
    /// Gets whether the validation was successful.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the error message if validation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    private SkillValidationResult(bool isValid, string? errorMessage)
    {
        IsValid = isValid;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static SkillValidationResult Success() => new(true, null);

    /// <summary>
    /// Creates a failed validation result with an error message.
    /// </summary>
    public static SkillValidationResult Failure(string errorMessage) => new(false, errorMessage);
}
