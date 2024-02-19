namespace BotSharp.Abstraction.Repositories.Filters;

public class ConversationFilter
{
    public Pagination Pager { get; set; } = new Pagination();
    /// <summary>
    /// Conversation Id
    /// </summary>
    public string? Id { get; set; }
    public string? AgentId { get; set; }
    public string? Status { get; set; }
    public string? Channel { get; set; }
    public string? UserId { get; set; }

    /// <summary>
    /// Agent task id
    /// </summary>
    public string? TaskId { get; set; }
}
