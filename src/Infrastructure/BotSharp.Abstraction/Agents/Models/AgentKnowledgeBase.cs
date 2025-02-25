namespace BotSharp.Abstraction.Agents.Models;

public class AgentKnowledgeBase
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool Disabled { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? Confidence { get; set; }

    public AgentKnowledgeBase()
    {
        
    }

    public AgentKnowledgeBase(
        string name, string type,
        bool enabled, decimal? confidence = null)
    {
        Name = name;
        Type = type;
        Disabled = enabled;
        Confidence = confidence;
    }

    public override string ToString()
    {
        return Name ?? string.Empty;
    }
}
