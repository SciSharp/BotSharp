namespace BotSharp.Abstraction.Agents.Models;

public class AgentResponse
{
    public string Prefix { get; set; }
    public string Intent { get; set; }
    public string Content { get; set; }

    public AgentResponse()
    {
        
    }

    public AgentResponse(string prefix, string intent, string content)
    {
        Prefix = prefix;
        Intent = intent;
        Content = content;
    }
}
