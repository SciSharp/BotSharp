namespace BotSharp.Abstraction.Agents.Models;

public class AgentCodeScript
{
    public string Id { get; set; }
    public string AgentId { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }

    public AgentCodeScript()
    {
    }

    public override string ToString()
    {
        return Name;
    }
}
