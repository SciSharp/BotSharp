namespace BotSharp.Abstraction.Repositories.Filters;

public class ConversationFilter
{
    public Pagination Pager { get; set; } = new Pagination();
    /// <summary>
    /// Conversation Id
    /// </summary>
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? TitleAlias { get; set; }
    public string? AgentId { get; set; }
    public string? Status { get; set; }
    public string? Channel { get; set; }
    public string? UserId { get; set; }
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Agent task id
    /// </summary>
    public string? TaskId { get; set; }

    /// <summary>
    /// Check whether each key in the list is in the conversation states and its value equals to target value if not empty 
    /// </summary>
    public IEnumerable<KeyValue>? States { get; set; } = [];

    public IEnumerable<string>? Tags { get; set; } = [];

    public static ConversationFilter Empty()
    {
        return new ConversationFilter();
    }
}