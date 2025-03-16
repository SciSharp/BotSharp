namespace BotSharp.Abstraction.Repositories.Filters;

public class ConversationLogFilter
{
    public int Size { get; set; } = 20;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    public ConversationLogFilter()
    {
        
    }

    public static ConversationLogFilter Empty()
    {
        return new();
    }
}
