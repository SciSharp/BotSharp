namespace BotSharp.Plugin.AgentSkills.Functions;

/// <summary>
/// Function that reads the full content of a skill's SKILL.md file.
/// </summary>
public class ReadSkillFn : IFunctionCallback
{
    public string Name => "skill-read_skill";
    public string Indication => "Reading skill instructions...";

    private readonly IServiceProvider _services;
    private readonly ILogger<ReadSkillFn> _logger;
    private readonly BotSharpOptions _options;

    public ReadSkillFn(
        IServiceProvider services,
        ILogger<ReadSkillFn> logger,
        BotSharpOptions options)
    {
        _services = services;
        _logger = logger;
        _options = options;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<ReadSkillArgs>(message.FunctionArgs, _options.JsonSerializerOptions);
        var skillName = args?.SkillName;

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
                    error = $"Skill '{skillName}' not found. Available skills: {string.Join(", ", state.AllSkills.Select(s => s.Name))}"
                }, _options.JsonSerializerOptions);
                return false;
            }

            var content = loader.ReadSkillContent(skill);
            message.Content = JsonSerializer.Serialize(new
            {
                success = true,
                skill_name = skill.Name,
                source = skill.Source.ToString().ToLowerInvariant(),
                path = skill.Path,
                content
            }, _options.JsonSerializerOptions);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read skill: {SkillName}", skillName);
            message.Content = JsonSerializer.Serialize(new
            {
                success = false,
                error = $"Failed to read skill '{skillName}': {ex.Message}"
            }, _options.JsonSerializerOptions);
            return false;
        }
    }

    private class ReadSkillArgs
    {
        [JsonPropertyName("skill_name")]
        public string? SkillName { get; set; }
    }
}
