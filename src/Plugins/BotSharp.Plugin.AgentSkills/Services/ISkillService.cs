using Microsoft.Extensions.AI;

namespace BotSharp.Plugin.AgentSkills.Services;

/// <summary>
/// Service interface for managing Agent Skills.
/// Encapsulates AgentSkillsDotNet library functionality and provides unified skill access.
/// Implements requirements: FR-1.1, FR-1.2, FR-1.3, FR-2.1, FR-3.1, NFR-4.1, NFR-4.2
/// </summary>
public interface ISkillService
{
    /// <summary>
    /// Gets all loaded skills.
    /// Implements requirement: FR-1.1 (Skill Discovery and Loading)
    /// </summary>
    /// <returns>The AgentSkills instance containing all loaded skills.</returns>
    /// <exception cref="InvalidOperationException">Thrown when skills are not loaded.</exception>
    IList<AgentSkills.Skills.AgentSkill> GetAgentSkills();

    /// <summary>
    /// Gets agent skills.
    /// </summary>
    /// <returns>The agent skills.</returns>
    /// <exception cref="InvalidOperationException">Thrown when skills are not loaded.</exception>
    IList<AgentSkills.Skills.AgentSkill> GetAgentSkills(Agent agent);
    
    /// <summary>
    /// Gets skill instructions text for injection into Agent prompts.
    /// Returns XML-formatted skill list compatible with Agent Skills specification.
    /// Implements requirement: FR-2.1 (Skill Metadata Injection)
    /// </summary>
    /// <returns>XML-formatted string containing available skills, or empty string if no skills loaded.</returns>
    string GetInstructions(Agent agent);
    
    /// <summary>
    /// Reloads all skills from configured directories.
    /// Useful for hot-reloading skills without restarting the application.
    /// Implements requirement: NFR-4.2 (Extensibility - Skill Reloading)
    /// </summary>
    /// <returns>A task representing the asynchronous reload operation.</returns>
    System.Threading.Tasks.Task ReloadSkillsAsync();
    
    /// <summary>
    /// Gets the count of loaded skills.
    /// Used for logging and monitoring purposes.
    /// Implements requirement: NFR-2.2 (Maintainability - Logging)
    /// </summary>
    /// <returns>The number of skills currently loaded, or 0 if no skills loaded.</returns>
    int GetSkillCount();
}
