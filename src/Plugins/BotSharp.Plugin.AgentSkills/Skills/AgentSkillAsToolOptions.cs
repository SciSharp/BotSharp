namespace BotSharp.Plugin.AgentSkills.Skills;

/// <summary>
/// Define how a Skill should report back its content as an AITool
/// </summary>
public class AgentSkillAsToolOptions
{
    /// <summary>
    /// Should the Description of the Skill be included
    /// </summary>
    public bool IncludeDescription { get; set; } = true;

    /// <summary>
    /// Should paths to script files be included
    /// </summary>
    public bool IncludeScriptFilesIfAny { get; set; } = true;

    /// <summary>
    /// Should paths to reference files be included
    /// </summary>
    public bool IncludeReferenceFilesIfAny { get; set; } = true;

    /// <summary>
    /// Should paths to asset files be included
    /// </summary>
    public bool IncludeAssetFilesIfAny { get; set; } = true;

    /// <summary>
    /// Should paths to other files be included
    /// </summary>
    public bool IncludeOtherFilesIfAny { get; set; } = true;

    /// <summary>
    /// Should any license information be included
    /// </summary>
    public bool IncludeLicenseInformation { get; set; }

    /// <summary>
    /// Should any compatibility information be included
    /// </summary>
    public bool IncludeCompatibilityInformation { get; set; }

    /// <summary>
    /// Should any metadata be included
    /// </summary>
    public bool IncludeMetadata { get; set; }

    /// <summary>
    /// Should any allowed tools information be included
    /// </summary>
    public bool IncludeAllowedTools { get; set; }
}
