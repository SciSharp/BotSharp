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
    /// Get the Agent skills as Tools
    /// </summary>
    /// <param name="strategy">Strategy for what set of tools should be returned</param>
    /// <param name="options">Options for the tools</param>
    /// <returns></returns>
    public IList<AITool> GetAsTools(AgentSkillsAsToolsStrategy strategy = AgentSkillsAsToolsStrategy.AvailableSkillsAndLookupTools, AgentSkillsAsToolsOptions? options = null)
    {
        IList<AITool> tools;
        AgentSkillsAsToolsOptions optionsToUse = options ?? new AgentSkillsAsToolsOptions();
        switch (strategy)
        {
            case AgentSkillsAsToolsStrategy.EachSkillAsATool:
                tools = Skills.Select(x => x.AsAITool(optionsToUse.AgentSkillAsToolOptions)).ToList();
                break;
            case AgentSkillsAsToolsStrategy.AvailableSkillsAndLookupTools:
                tools = GetAsLookupToolAndSpecificsTools(optionsToUse);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null);
        }

        if (optionsToUse.IncludeToolForFileContentRead)
        {
            tools.Add(GetSkillFileContentTool(optionsToUse));
        }

        return tools;
    }

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

    private IList<AITool> GetAsLookupToolAndSpecificsTools(AgentSkillsAsToolsOptions options)
    {
        List<AITool> tools = [];
        string availableSkillToolDefinition = GetInstructions();
        tools.Add(AIFunctionFactory.Create(
            method: () => availableSkillToolDefinition,
            name: options.GetAvailableSkillToolName,
            description: options.GetAvailableSkillToolDescription));

        tools.Add(AIFunctionFactory.Create(
            method: (string skillName) =>
            {
                AgentSkill? skill = Skills.FirstOrDefault(x => x.Name.Equals(skillName, StringComparison.CurrentCultureIgnoreCase));
                return skill != null ? skill.GenerateDefinition(options.AgentSkillAsToolOptions) : $"Error: Skill with name '{skillName}' was not found";
            },
            name: options.GetSpecificSkillToolName,
            description: options.GetSpecificSkillToolDescription));
        return tools;
    }

    private AITool GetSkillFileContentTool(AgentSkillsAsToolsOptions options)
    {
        IEnumerable<string> allowedFiles = Skills.SelectMany(x => x.AssetFiles.Union(x.OtherFiles).Union(x.ScriptFiles).Union(x.ReferenceFiles));

        AIFunction function = AIFunctionFactory.Create(
            method: (string filePath) =>
            {
                if (!allowedFiles.Contains(filePath))
                {
                    return $"Error: File '{filePath}' is not a valid Skill-file";
                }

                return File.ReadAllText(filePath, Encoding.UTF8);
            },
            name: options.ReadSkillFileContentToolName,
            description: options.ReadSkillFileContentToolDescription);

        return function;
    }
}
