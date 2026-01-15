namespace BotSharp.Plugin.AgentSkills.Functions;

/// <summary>
/// Function that reads a file within a skill's directory.
/// </summary>
public class ReadSkillFileFn : IFunctionCallback
{
    public string Name => "skill-read_skill_file";
    public string Indication => "Reading skill file...";

    private readonly IServiceProvider _services;
    private readonly ILogger<ReadSkillFileFn> _logger;
    private readonly BotSharpOptions _options;

    public ReadSkillFileFn(
        IServiceProvider services,
        ILogger<ReadSkillFileFn> logger,
        BotSharpOptions options)
    {
        _services = services;
        _logger = logger;
        _options = options;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ReadSkillFileArgs>(message.FunctionArgs, _options.JsonSerializerOptions);
        var skillName = args?.SkillName;
        var filePath = args?.FilePath;

        if (string.IsNullOrWhiteSpace(skillName))
        {
            message.Content = JsonSerializer.Serialize(new
            {
                success = false,
                error = "Skill name is required."
            }, _options.JsonSerializerOptions);
            return false;
        }

        if (string.IsNullOrWhiteSpace(filePath))
        {
            message.Content = JsonSerializer.Serialize(new
            {
                success = false,
                error = "File path is required."
            }, _options.JsonSerializerOptions);
            return false;
        }

        try
        {
            var settings = _services.GetRequiredService<AgentSkillsSettings>();
            var loader = _services.GetRequiredService<SkillLoader>();

            // Load skills and find the requested one
            var state = loader.LoadSkills(settings);
            var skill = state.GetSkill(skillName);

            if (skill is null)
            {
                message.Content = JsonSerializer.Serialize(new
                {
                    success = false,
                    error = $"Skill '{skillName}' not found."
                }, _options.JsonSerializerOptions);
                return false;
            }

            var content = loader.ReadSkillFile(skill, filePath);

            // Truncate if necessary
            var maxSize = settings.MaxOutputSizeBytes;
            var originalLength = content.Length;
            var truncated = content.Length > maxSize;
            if (truncated)
            {
                content = content.Substring(0, maxSize);
            }

            message.Content = JsonSerializer.Serialize(new
            {
                success = true,
                skill_name = skill.Name,
                file_path = filePath,
                content,
                truncated,
                total_length = originalLength
            }, _options.JsonSerializerOptions);

            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Path traversal attempt for skill: {SkillName}", skillName);
            message.Content = JsonSerializer.Serialize(new
            {
                success = false,
                error = "Access denied: path traversal detected."
            }, _options.JsonSerializerOptions);
            return false;
        }
        catch (FileNotFoundException)
        {
            message.Content = JsonSerializer.Serialize(new
            {
                success = false,
                error = $"File not found: {filePath}"
            }, _options.JsonSerializerOptions);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read skill file: {SkillName}/{FilePath}", skillName, filePath);
            message.Content = JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Failed to read file: {ex.Message}"
            }, _options.JsonSerializerOptions);
            return false;
        }
    }

    private class ReadSkillFileArgs
    {
        [JsonPropertyName("skill_name")]
        public string? SkillName { get; set; }

        [JsonPropertyName("file_path")]
        public string? FilePath { get; set; }
    }
}
