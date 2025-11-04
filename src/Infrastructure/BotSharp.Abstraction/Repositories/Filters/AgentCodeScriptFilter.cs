namespace BotSharp.Abstraction.Repositories.Filters;

public class AgentCodeScriptFilter
{
    public List<string>? ScriptNames { get; set; }
    public List<string>? ScriptTypes { get; set; }

    public static AgentCodeScriptFilter Empty()
    {
        return new();
    }
}
