namespace BotSharp.Plugin.WebDriver.Models;

public class BrowserActionParams
{
    public Agent Agent { get; set; }
    public BrowsingContextIn Context { get; set; }
    public string MessageId { get; set; }

    public BrowserActionParams(Agent agent, BrowsingContextIn context, string messageId)
    {
        Agent = agent;
        Context = context;
        MessageId = messageId;
    }
}
