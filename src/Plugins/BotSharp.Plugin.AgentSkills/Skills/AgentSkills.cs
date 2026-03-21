using System.Text;
using Microsoft.Extensions.AI;

namespace BotSharp.Plugin.AgentSkills.Skills;

/// <summary>
/// Represent a set of AgentSkills
/// </summary>
public class AgentSkills
{
    /// <summary>
    /// AgentSkills that are valid
    /// </summary>
    public required IList<AgentSkill> Skills { get; set; }

    /// <summary>
    /// Log of why certain skills was excluded (due to validation failure or Filtering)
    /// </summary>
    public IList<string> ExcludedSkillsLog { get; set; } = [];     

    /// <summary>
    /// Get a definition of what skills are available (to include in AI Instructions)
    /// </summary>
    /// <returns>Definition</returns>
    public string GetInstructions()
    {
        StringBuilder availableSkillToolBuilder = new();
        availableSkillToolBuilder.AppendLine("<available_skills>");
        foreach (AgentSkill skill in Skills)
        {
            availableSkillToolBuilder.AppendLine("\t<skill>");
            availableSkillToolBuilder.AppendLine($"\t\t<name>{skill.Name}</name>");
            availableSkillToolBuilder.AppendLine($"\t\t<description>{skill.Description}</description>");
            availableSkillToolBuilder.AppendLine($"\t\t<location>{skill.FolderPath}</location>");
            availableSkillToolBuilder.AppendLine("\t</skill>");
        }

        availableSkillToolBuilder.AppendLine("</available_skills>");
        return availableSkillToolBuilder.ToString();
    }    
}
