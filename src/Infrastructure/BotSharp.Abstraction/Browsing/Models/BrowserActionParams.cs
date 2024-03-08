namespace BotSharp.Abstraction.Browsing.Models;

public class BrowserActionParams
{
    public Agent Agent { get; set; }
    public BrowsingContextIn Context { get; set; }
    public string ConversationId { get; set; }
    public string MessageId { get; set; }

    public BrowserActionParams(Agent agent, BrowsingContextIn context, string conversationId, string messageId)
    {
        Agent = agent;
        Context = context;
        ConversationId = conversationId;
        MessageId = messageId;
    }
}
