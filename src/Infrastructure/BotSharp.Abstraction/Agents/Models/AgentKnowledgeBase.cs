namespace BotSharp.Abstraction.Agents.Models;

public class AgentKnowledgeBase
{
    public string Name { get; set; }
    public string Type { get; set; }
    public bool Disabled { get; set; }

    public AgentKnowledgeBase()
    {
        
    }

    public AgentKnowledgeBase(string name, string type, bool enabled)
    {
        Name = name;
        Type = type;
        Disabled = enabled;
    }

    public override string ToString()
    {
        return Name ?? string.Empty;
    }
}
