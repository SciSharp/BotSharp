namespace BotSharp.Abstraction.Agents.Models;

public class AgentSkill
{
    /// <summary>
    /// Name of the Skill
    /// </summary>
    [JsonPropertyName("name")] 
    public required string Name { get; set; }

    /// <summary>
    /// Description of the Skill
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; set; }
}
