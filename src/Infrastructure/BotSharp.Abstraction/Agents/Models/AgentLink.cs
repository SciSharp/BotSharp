namespace BotSharp.Abstraction.Agents.Models;

public class AgentLink : AgentPromptBase
{
    public AgentLink() : base()
    {
    }

    public AgentLink(string name, string content) : base(name, content)
    {
    }

    public override string ToString()
    {
        return base.ToString();
    }
}
