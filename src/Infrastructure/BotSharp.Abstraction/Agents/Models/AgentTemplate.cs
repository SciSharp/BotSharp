namespace BotSharp.Abstraction.Agents.Models;

public class AgentTemplate
{
    public string Name { get; set; }
    public string Content { get; set; }

    public AgentTemplate()
    {
        
    }

    public AgentTemplate(string name, string content)
    {
        Name = name;
        Content = content;
    }
}
