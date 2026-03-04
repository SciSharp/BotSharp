namespace BotSharp.Plugin.AgentSkills.Skills;

/// <summary>
/// What set of AI Tools should be generated
/// </summary>
public enum AgentSkillsAsToolsStrategy
{
    /// <summary>
    /// Each AgentSkill should be its own tool
    /// </summary>
    EachSkillAsATool,

    /// <summary>
    /// Generate 2 tools (One to list available skills, and one for getting a specific skill details
    /// </summary>
    AvailableSkillsAndLookupTools
}
