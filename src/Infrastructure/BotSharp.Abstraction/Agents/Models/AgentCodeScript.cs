namespace BotSharp.Abstraction.Agents.Models;

public class AgentCodeScript
{
    public string Name { get; set; }
    public string Content { get; set; }

    public AgentCodeScript()
    {
    }

    public AgentCodeScript(string name, string content)
    {
        Name = name;
        Content = content;
    }

    public override string ToString()
    {
        return Name;
    }
}
