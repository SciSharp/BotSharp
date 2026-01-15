namespace BotSharp.Plugin.AgentSkills.Functions;

/// <summary>
/// Function that lists contents of a skill's directory.
/// </summary>
public class ListSkillDirectoryFn : IFunctionCallback
{
    public string Name => "skill-list_skill_directory";
    public string Indication => "Listing skill directory...";

    private readonly IServiceProvider _services;
    private readonly ILogger<ListSkillDirectoryFn> _logger;
    private readonly BotSharpOptions _options;

    public ListSkillDirectoryFn(
        IServiceProvider services,
        ILogger<ListSkillDirectoryFn> logger,
        BotSharpOptions options)
    {
        _services = services;
        _logger = logger;
        _options = options;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ListSkillDirectoryArgs>(message.FunctionArgs, _options.JsonSerializerOptions);
        var skillName = args?.SkillName;
        var relativePath = args?.RelativePath;

        if (string.IsNullOrWhiteSpace(skillName))
        {
            message.Content = JsonSerializer.Serialize(new
            {
                success = false,
                error = "Skill name is required."
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

            var entries = loader.ListSkillDirectory(skill, relativePath).ToList();

            message.Content = JsonSerializer.Serialize(new
            {
                success = true,
                skill_name = skill.Name,
                path = relativePath ?? "/",
                entries = entries.Select(e => new
                {
                    name = e.Name,
                    type = e.IsDirectory ? "directory" : "file",
                    size = e.Size
                })
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list skill directory: {SkillName}/{Path}", skillName, relativePath);
            message.Content = JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Failed to list directory: {ex.Message}"
            }, _options.JsonSerializerOptions);
            return false;
        }
    }

    private class ListSkillDirectoryArgs
    {
        [JsonPropertyName("skill_name")]
        public string? SkillName { get; set; }

        [JsonPropertyName("relative_path")]
        public string? RelativePath { get; set; }
    }
}
