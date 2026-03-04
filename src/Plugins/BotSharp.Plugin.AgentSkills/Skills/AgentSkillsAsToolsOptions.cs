namespace BotSharp.Plugin.AgentSkills.Skills;

/// <summary>
/// The options for the generated tools
/// </summary>
public class AgentSkillsAsToolsOptions
{
    /// <summary>
    /// get-available-skills
    /// </summary>
    public const string DefaultGetAvailableSkillToolName = "get-available-skills";

    /// <summary>
    /// get-skill-by-name
    /// </summary>
    public const string DefaultGetSpecificSkillToolName = "get-skill-by-name";

    /// <summary>
    /// read-skill-file-content
    /// </summary>
    public const string DefaultReadSkillFileContentToolName = "read-skill-file-content";

    /// <summary>
    /// If a tool for reading AgentSkill Files should be included or not (such a tool can only read files originating from the skills folder)
    /// </summary>
    public bool IncludeToolForFileContentRead { get; set; } = true;

    /// <summary>
    /// Name of the tool to list available skills (default: get-available-skills) [Only generated if 'AvailableSkillsAndLookupTools' strategy was used]
    /// </summary>
    public string GetAvailableSkillToolName { get; set; } = DefaultGetAvailableSkillToolName;

    /// <summary>
    /// Description of the tool to list available skill tool (default: 'Get a list of the available skills') [Only generated if 'AvailableSkillsAndLookupTools' strategy was used]
    /// </summary>
    public string GetAvailableSkillToolDescription { get; set; } = "Get a list of the available skills";

    /// <summary>
    /// Name of the tool to get the specific skill (default: get-skill-by-name) [Only generated if 'AvailableSkillsAndLookupTools' strategy was used]
    /// </summary>
    public string GetSpecificSkillToolName { get; set; } = DefaultGetSpecificSkillToolName;

    /// <summary>
    /// Description of the tool to get the specific skill (default: 'Get a specific skill by its name') [Only generated if 'AvailableSkillsAndLookupTools' strategy was used]
    /// </summary>
    public string GetSpecificSkillToolDescription { get; set; } = "Get a specific skill by its name";

    /// <summary>
    /// Name of the tool to read file content (Default: 'read-skill-file-content')
    /// </summary>
    public string ReadSkillFileContentToolName { get; set; } = DefaultReadSkillFileContentToolName;

    /// <summary>
    /// Description of the tool to read file content (Default: 'Read the content of a Skill File by its path')
    /// </summary>
    public string ReadSkillFileContentToolDescription { get; set; } = "Read the content of a Skill File by its path";

    /// <summary>
    /// Options on how the specific tool should be returned
    /// </summary>
    public AgentSkillAsToolOptions? AgentSkillAsToolOptions { get; set; }
}
