namespace BotSharp.Abstraction.Repositories.Filters;

public class AgentTaskFilter
{
    public Pagination Pager { get; set; } = new Pagination();
    public string? AgentId { get; set; }
    public bool? Enabled { get; set; }
    public string? Status { get; set; }

    public static AgentTaskFilter Empty()
    {
        return new AgentTaskFilter();
    }
}
