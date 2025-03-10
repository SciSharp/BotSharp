namespace BotSharp.Abstraction.Repositories.Filters;

public class ConversationStateKeysFilter
{
    public string? Query { get; set; }
    public int KeyLimit { get; set; } = 10;
    public int ConvLimit { get; set; } = 100;
    public bool PreLoad { get; set; }
    public List<string>? AgentIds { get; set; }
    public List<string>? UserIds { get; set; }

    public ConversationStateKeysFilter()
    {
        
    }

    public static ConversationStateKeysFilter Empty()
    {
        return new();   
    }
}
