namespace BotSharp.Abstraction.Repositories.Filters;

public class InstructLogFilter : Pagination
{
    public List<string>? AgentIds { get; set; }
    public List<string>? Providers { get; set; }
    public List<string>? Models { get; set; }
    public List<string>? TemplateNames { get; set; }
    public List<string>? UserIds { get; set; }
    public List<KeyValue>? States { get; set; }

    public static InstructLogFilter Empty()
    {
        return new();
    }
}
