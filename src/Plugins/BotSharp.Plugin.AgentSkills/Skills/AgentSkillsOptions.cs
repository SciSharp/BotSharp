namespace BotSharp.Plugin.AgentSkills.Skills;

/// <summary>
/// Options for when getting Agent Skills
/// </summary>
public class AgentSkillsOptions
{
    /// <summary>
    /// Filter to apply when choosing skills (return true to include or false to exclude) [Note: Validation-rules apply BEFORE Filtering]
    /// </summary>
    public Func<AgentSkill, bool>? Filter { get; set; }

    /// <summary>
    /// Validation Rules for if a Skill is valid and be returned [NOTE: Filtering happens AFTER this validation]
    /// </summary>
    public AgentSkillsOptionsValidationRule ValidationRules { get; set; } = AgentSkillsOptionsValidationRule.Strict;
}
