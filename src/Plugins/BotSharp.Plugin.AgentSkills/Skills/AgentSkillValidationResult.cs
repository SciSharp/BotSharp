namespace BotSharp.Plugin.AgentSkills.Skills;

/// <summary>
/// An AgentSkill Validation Result (if it follows the spec)
/// </summary>
public class AgentSkillValidationResult
{
    /// <summary>
    /// If the skill is valid
    /// </summary>
    public required bool Valid { get; set; }

    /// <summary>
    /// If not valid, what type of issues was detected
    /// </summary>
    public required string[] Issues { get; set; }
}
