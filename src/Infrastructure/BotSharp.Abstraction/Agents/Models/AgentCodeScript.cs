namespace BotSharp.Abstraction.Agents.Models;

public class AgentCodeScript : AgentCodeScriptBase
{
    public string Id { get; set; }
    public string AgentId { get; set; } = null!;

    public AgentCodeScript() : base()
    {
    }

    public override string ToString()
    {
        return base.ToString();
    }
}

public class AgentCodeScriptBase
{
    public string Name { get; set; } = null!;
    public string Content { get; set; } = null!;

    /// <summary>
    /// Code script type: src, test
    /// </summary>
    public string ScriptType { get; set; } = null!;

    public string CodePath => $"{ScriptType}/{Name}";

    public override string ToString()
    {
        return $"{CodePath}";
    }
}