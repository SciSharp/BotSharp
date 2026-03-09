using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace BotSharp.Plugin.AgentSkills.Skills;

/// <summary>
/// Represent an Agent Skill
/// </summary>
public class AgentSkill
{
    /// <summary>
    /// Name of the parent Folder Path
    /// </summary>
    public required string FolderPath { get; set; }

    /// <summary>
    /// Name of the Skill
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Description of the Skill
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// License information
    /// </summary>
    public string? License { get; set; }

    /// <summary>
    /// Compatibility Information
    /// </summary>
    public string? Compatibility { get; set; }

    /// <summary>
    /// Metadata
    /// </summary>
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Body of the skill
    /// </summary>
    public string? Body { get; set; }

    /// <summary>
    /// Script Files associated with the skill
    /// </summary>
    public required string[] ScriptFiles { get; set; }

    /// <summary>
    /// Reference Files (aka additional documentation) associated with the skill
    /// </summary>
    public required string[] ReferenceFiles { get; set; }

    /// <summary>
    /// Asset Files associated with the skill
    /// </summary>
    public required string[] AssetFiles { get; set; }

    /// <summary>
    /// Other Files associated with the skill
    /// </summary>
    public required string[] OtherFiles { get; set; }

    /// <summary>
    /// What tools are allowed [Experimental field]
    /// </summary>
    public string? AllowedTools { get; set; }

    /// <summary>
    /// Summary of if the AgentSkill is valid (follow specification)
    /// </summary>
    public AgentSkillValidationResult GetValidationResult()
    {
        bool valid = true;
        List<string> issues = [];

        if (string.IsNullOrWhiteSpace(Name))
        {
            valid = false;
            issues.Add("Name: Not specified");
        }
        else
        {
            if (Name != Path.GetFileNameWithoutExtension(FolderPath))
            {
                valid = false;
                issues.Add("Name: Must match the parent directory name");
            }

            if (Name.Contains("--"))
            {
                valid = false;
                issues.Add("Name: Must not contain consecutive hyphens (--)");
            }

            if (Name.Length > 64)
            {
                valid = false;
                issues.Add("Name: Must be 1-64 characters");
            }

            if (!Regex.IsMatch(Name, "^[a-z0-9-]+$"))
            {
                valid = false;
                issues.Add("Name: May only contain unicode lowercase alphanumeric characters and hyphens (a-z and -)");
            }
        }

        if (string.IsNullOrWhiteSpace(Description))
        {
            valid = false;
            issues.Add("Description: Not specified");
        }
        else
        {
            if (Name.Length > 1024)
            {
                valid = false;
                issues.Add("Description: Must be 1-1024 characters");
            }
        }

        return new AgentSkillValidationResult
        {
            Valid = valid,
            Issues = issues.ToArray()
        };
    }


    /// <summary>
    /// Get the Definition of the Skill
    /// </summary>
    /// <param name="options">Options for the definition</param>
    /// <returns>The Definition</returns>
    public string GenerateDefinition(AgentSkillAsToolOptions? options = null)
    {
        AgentSkillAsToolOptions optionsToUse = options ?? new AgentSkillAsToolOptions();
        StringBuilder builder = new();
        if (optionsToUse.IncludeDescription)
        {
            builder.AppendLine($"<skill name=\"{Name}\" description=\"{Description}\">");
        }
        else
        {
            builder.AppendLine($"<skill name=\"{Name}\">");
        }

        builder.AppendLine("<instructions>");
        builder.AppendLine(Body);
        builder.AppendLine("</instructions>");

        if (optionsToUse.IncludeLicenseInformation && string.IsNullOrWhiteSpace(License))
        {
            builder.AppendLine($"<license>{License}</license>");
        }

        if (optionsToUse.IncludeCompatibilityInformation && string.IsNullOrWhiteSpace(Compatibility))
        {
            builder.AppendLine($"<compatibility>{Compatibility}</compatibility>");
        }

        if (optionsToUse.IncludeMetadata && Metadata?.Count > 0)
        {
            builder.AppendLine("<metadata>");
            foreach (KeyValuePair<string, string> keyValuePair in Metadata)
            {
                builder.AppendLine($"<{keyValuePair.Key}>{keyValuePair.Value}</{keyValuePair.Key}>");
            }

            builder.AppendLine("</metadata>");
        }

        if (optionsToUse.IncludeAllowedTools && string.IsNullOrWhiteSpace(AllowedTools))
        {
            builder.AppendLine($"<allowedTools>{AllowedTools}</allowedTools>");
        }

        IncludeFileSection(optionsToUse.IncludeScriptFilesIfAny, ScriptFiles, "scriptFiles");
        IncludeFileSection(optionsToUse.IncludeReferenceFilesIfAny, ReferenceFiles, "referenceFiles");
        IncludeFileSection(optionsToUse.IncludeAssetFilesIfAny, AssetFiles, "assetFiles");
        IncludeFileSection(optionsToUse.IncludeOtherFilesIfAny, OtherFiles, "otherFiles");
        builder.AppendLine("</skill>");
        string definition = builder.ToString();
        return definition;

        void IncludeFileSection(bool include, string[] files, string plural)
        {
            if (include && files.Length != 0)
            {
                builder.AppendLine($"<{plural}>");
                foreach (string scriptFile in files)
                {
                    builder.AppendLine($"<file>{scriptFile}</file>");
                }

                builder.AppendLine($"</{plural}>");
            }
        }
    }

    internal string GetDisplayName()
    {
        return !string.IsNullOrWhiteSpace(Name) ? Name : FolderPath;
    }
}
