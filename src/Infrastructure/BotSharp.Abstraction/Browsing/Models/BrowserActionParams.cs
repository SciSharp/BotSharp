namespace BotSharp.Abstraction.Browsing.Models;

[Obsolete("This class is deprecated, use BrowserActionArgs instead.")]
public class BrowserActionParams
{
    public Agent Agent { get; set; }
    public BrowsingContextIn Context { get; set; }
    public string ContextId { get; set; }
    public string MessageId { get; set; }

    public BrowserActionParams(Agent agent, BrowsingContextIn context, string contextId, string messageId)
    {
        Agent = agent;
        Context = context;
        ContextId = contextId;
        MessageId = messageId;
    }
}
