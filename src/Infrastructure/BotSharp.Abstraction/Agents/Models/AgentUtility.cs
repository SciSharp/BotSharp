namespace BotSharp.Abstraction.Agents.Models;

public class AgentUtility
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("functions")]
    public IEnumerable<UtilityFunction> Functions { get; set; } = [];

    [JsonPropertyName("templates")]
    public IEnumerable<UtilityTemplate> Templates { get; set; } = [];

    public AgentUtility()
    {
        
    }

    public AgentUtility(
        string name,
        IEnumerable<UtilityFunction>? functions = null,
        IEnumerable<UtilityTemplate>? templates = null)
    {
        Name = name;
        Functions = functions ?? [];
        Templates = templates ?? [];
    }
}


public class UtilityFunction : UtilityBase
{
    public UtilityFunction()
    {
        
    }

    public UtilityFunction(string name)
    {
        Name = name;
    }
}

public class UtilityTemplate : UtilityBase
{
    public UtilityTemplate()
    {
        
    }

    public UtilityTemplate(string name)
    {
        Name = name;
    }
}

public class UtilityBase
{
    public string Name { get; set; }
}