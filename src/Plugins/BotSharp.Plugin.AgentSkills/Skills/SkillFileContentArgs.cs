using System.Text.Json.Serialization;

namespace BotSharp.Plugin.AgentSkills.Skills;

public class SkillFileContentArgs
{
    [JsonPropertyName("filepath")]
    public string FilePath { get; set; } = string.Empty;
}
