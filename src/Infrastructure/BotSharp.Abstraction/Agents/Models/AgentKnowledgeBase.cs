namespace BotSharp.Abstraction.Agents.Models;

public class AgentKnowledgeBase
{
    public string? Name { get; set; }
    public bool Disabled { get; set; }

    public AgentKnowledgeBase()
    {
        
    }

    public AgentKnowledgeBase(string name, bool enabled)
    {
        Name = name;
        Disabled = enabled;
    }

    public override string ToString()
    {
        return Name ?? string.Empty;
    }
}
