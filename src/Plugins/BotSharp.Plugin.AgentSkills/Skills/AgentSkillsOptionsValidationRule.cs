namespace BotSharp.Plugin.AgentSkills.Skills;

/// <summary>
/// The Validation Rule on whether to include a skill or not
/// </summary>
public enum AgentSkillsOptionsValidationRule
{
    /// <summary>
    /// In order to include the skill all rules in https://agentskills.io/specification must be met 
    /// </summary>
    Strict,

    /// <summary>
    /// Include tool if it has a name (else exclude it)
    /// </summary>
    Loose,

    /// <summary>
    /// No validation; include not matter what (if no name, folder-name is used as AITool name)
    /// </summary>
    None
}
