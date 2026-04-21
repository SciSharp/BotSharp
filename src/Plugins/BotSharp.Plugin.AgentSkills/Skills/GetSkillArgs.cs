using System.Text.Json.Serialization;

namespace BotSharp.Plugin.AgentSkills.Skills;

public class GetSkillArgs
{
    [JsonPropertyName("skillname")]
    public string SkillName { get; set; } = string.Empty;
}
