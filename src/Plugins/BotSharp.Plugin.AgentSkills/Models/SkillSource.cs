namespace BotSharp.Plugin.AgentSkills.Models;

/// <summary>
/// Represents the source location of a skill.
/// </summary>
public enum SkillSource
{
    /// <summary>
    /// User-level skill stored in ~/.botsharp/skills/
    /// </summary>
    User,

    /// <summary>
    /// Project-level skill stored in {project}/.botsharp/skills/
    /// </summary>
    Project
}
