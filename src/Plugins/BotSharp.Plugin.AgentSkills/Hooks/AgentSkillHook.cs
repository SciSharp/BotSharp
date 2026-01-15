using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Agents.Settings;

namespace BotSharp.Plugin.AgentSkills.Hooks;

public class AgentSkillHook : AgentHookBase
{
    public override string SelfId => string.Empty;

    private readonly SkillLoader _skillLoader;
    private readonly AgentSkillsSettings _options;
    private SkillsState _state;

    public AgentSkillHook(IServiceProvider services, AgentSettings settings)
        : base(services, settings)
    {
        _skillLoader = services.GetRequiredService<SkillLoader>();
        _options = services.GetRequiredService<AgentSkillsSettings>();
        _state = _skillLoader.LoadSkills(_options);
    }

    public override bool OnInstructionLoaded(string template, IDictionary<string, object> dict)
    {
        if (Agent.Type == AgentType.Routing || Agent.Type == AgentType.Planning)
        {
            return base.OnInstructionLoaded(template, dict);
        }

        // Refresh skills if needed or if this is the first load
        if (_state.AllSkills.Count == 0)
        {
            LoadSkills(_options);
        }

        var skillsList = GenerateSkillsList(_state);

        var locations = $"- **User Skills**: `{_options.UserSkillsDir}`";
        if (_options.ProjectSkillsDir != null)
        {
                locations += Environment.NewLine + $"- **Project Skills**: `{_options.ProjectSkillsDir}`";
        }
        dict["skills_locations"] = locations;
        dict["skills_list"] = skillsList;

        return base.OnInstructionLoaded(template, dict);
    }

    /// <summary>
    /// Loads skills from configured directories.
    /// </summary>
    private void LoadSkills(AgentSkillsSettings settings)
    {
        var skills = new Dictionary<string, SkillMetadata>(StringComparer.OrdinalIgnoreCase);

        // Load user-level skills
        var userDir = settings.GetUserSkillsDirectory();
        if (Directory.Exists(userDir))
        {
            foreach (var skill in _skillLoader.LoadSkillsFromDirectory(userDir, SkillSource.User))
            {
                skills[skill.Name] = skill;
            }
        }

        // Load project-level skills (overrides user-level with same name)
        var projectDir = settings.GetProjectSkillsDirectory();
        if (projectDir != null && Directory.Exists(projectDir))
        {
            foreach (var skill in _skillLoader.LoadSkillsFromDirectory(projectDir, SkillSource.Project))
            {
                skills[skill.Name] = skill;
            }
        }

        _state = new SkillsState
        {
            UserSkills = skills.Values.Where(s => s.Source == SkillSource.User).ToList(),
            ProjectSkills = skills.Values.Where(s => s.Source == SkillSource.Project).ToList(),
            LastRefreshed = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Generates a formatted list of available skills.
    /// </summary>
    /// <param name="state">The current skills state.</param>
    /// <returns>The formatted skills list.</returns>
    public static string GenerateSkillsList(SkillsState state)
    {
        var lines = new List<string>();

        // Group by source for clarity
        if (state.ProjectSkills.Count > 0)
        {
            lines.Add("*Project Skills:*");
            foreach (var skill in state.ProjectSkills)
            {
                lines.Add(skill.ToDisplayString());
            }
        }

        if (state.UserSkills.Count > 0)
        {
            // Filter out user skills that are overridden by project skills
            var projectSkillNames = state.ProjectSkills
                .Select(s => s.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var nonOverriddenUserSkills = state.UserSkills
                .Where(s => !projectSkillNames.Contains(s.Name))
                .ToList();

            if (nonOverriddenUserSkills.Count > 0)
            {
                if (lines.Count > 0)
                {
                    lines.Add("");
                }
                lines.Add("*User Skills:*");
                foreach (var skill in nonOverriddenUserSkills)
                {
                    lines.Add(skill.ToDisplayString());
                }
            }
        }

        if (lines.Count == 0)
        {
            return "*No skills available.*";
        }

        return string.Join(Environment.NewLine, lines);
    }
}