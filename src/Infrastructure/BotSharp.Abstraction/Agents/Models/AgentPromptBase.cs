namespace BotSharp.Abstraction.Agents.Models;

public class AgentPromptBase
{
    public string Name { get; set; }
    public string Content { get; set; }

    public AgentPromptBase()
    {
        
    }

    public AgentPromptBase(string name, string content)
    {
        Name = name;
        Content = content;
    }

    public override string ToString()
    {
        return Name;
    }
}
