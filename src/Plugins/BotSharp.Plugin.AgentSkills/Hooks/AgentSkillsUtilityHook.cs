using BotSharp.Abstraction.Agents;

namespace BotSharp.Plugin.AgentSkills.Hooks;

/// <summary>
/// Hook that adds Agent Skills utilities to agents.
/// </summary>
public class AgentSkillsUtilityHook : IAgentUtilityHook
{
    private static readonly string PREFIX = "skill-";
    private static readonly string READ_SKILL_FN = $"{PREFIX}read_skill";
    private static readonly string READ_SKILL_FILE_FN = $"{PREFIX}read_skill_file";
    private static readonly string LIST_SKILL_DIRECTORY_FN = $"{PREFIX}list_skill_directory";

    public void AddUtilities(List<AgentUtility> utilities)
    {
        var utility = new AgentUtility
        {
            Category = "skill",
            Name = UtilityName.AgentSkills,
            Items = [
                new UtilityItem
                {
                    FunctionName = READ_SKILL_FN,
                    TemplateName = $"{READ_SKILL_FN}.fn",
                    Description = "Reads the full content of a skill's SKILL.md file to get detailed instructions."
                },
                new UtilityItem
                {
                    FunctionName = READ_SKILL_FILE_FN,
                    TemplateName = $"{READ_SKILL_FILE_FN}.fn",
                    Description = "Reads a file within a skill's directory."
                },
                new UtilityItem
                {
                    FunctionName = LIST_SKILL_DIRECTORY_FN,
                    TemplateName = $"{LIST_SKILL_DIRECTORY_FN}.fn",
                    Description = "Lists the contents of a skill's directory."
                }
            ]
        };

        utilities.Add(utility);
    }
}
