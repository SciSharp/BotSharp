namespace BotSharp.Plugin.AgentSkills.Skills;

/// <summary>
/// A Factory for producing AgentSkills
/// </summary>
public class AgentSkillsFactory
{
    /// <summary>
    /// Get a set of Agent Skills from the given folder and its sub-folders of skills
    /// </summary>
    /// <param name="folderPath">The Local folder with skills sub-folders</param>
    /// <param name="options">Options when getting skills</param>
    /// <returns>The skills found</returns>
    public AgentSkills GetAgentSkills(string folderPath, AgentSkillsOptions? options = null)
    {
        AgentSkillsOptions optionsToUse = options ?? new AgentSkillsOptions();
        List<AgentSkill> skills = [];
        List<string> excludedSkillsLog = [];
        AgentSkillReader reader = new();
        string[] skillFiles = Directory.GetFiles(folderPath, "SKILL.md", new EnumerationOptions
        {
            MatchCasing = MatchCasing.CaseInsensitive,
            RecurseSubdirectories = true
        });
        foreach (string skillFile in skillFiles)
        {
            AgentSkill skill = reader.ReadSkill(skillFile);
            bool include;
            switch (optionsToUse.ValidationRules)
            {
                case AgentSkillsOptionsValidationRule.Strict:
                    AgentSkillValidationResult validationResult = skill.GetValidationResult();
                    include = validationResult.Valid;
                    if (!include)
                    {
                        excludedSkillsLog.Add($"Skill: {skill.GetDisplayName()} was exclude as it did not follow agent-skills spec [{string.Join(", ", validationResult.Issues)}]");
                    }

                    break;
                case AgentSkillsOptionsValidationRule.Loose:
                    include = !string.IsNullOrWhiteSpace(skill.Name);
                    if (!include)
                    {
                        excludedSkillsLog.Add($"Skill: {skill.GetDisplayName()} was exclude as it did not have a name");
                    }

                    break;
                case AgentSkillsOptionsValidationRule.None:
                    include = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(excludedSkillsLog));
            }

            if (optionsToUse.Filter != null)
            {
                include = optionsToUse.Filter.Invoke(skill);
                if (!include)
                {
                    excludedSkillsLog.Add($"Skill: {skill.GetDisplayName()} was exclude as it did not fit Filter");
                }
            }

            if (include)
            {
                skills.Add(skill);
            }
        }

        return new AgentSkills
        {
            Skills = skills.ToArray(),
            ExcludedSkillsLog = excludedSkillsLog.ToArray()
        };
    }
}
