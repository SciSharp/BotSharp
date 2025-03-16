namespace BotSharp.Abstraction.Repositories.Filters;

public class InstructLogKeysFilter
{
    public string? Query { get; set; }
    public int KeyLimit { get; set; } = 10;
    public int LogLimit { get; set; } = 100;
    public bool PreLoad { get; set; }
    public List<string>? AgentIds { get; set; }
    public List<string>? UserIds { get; set; }

    public InstructLogKeysFilter()
    {
        
    }

    public static InstructLogKeysFilter Empty()
    {
        return new();
    }
}
