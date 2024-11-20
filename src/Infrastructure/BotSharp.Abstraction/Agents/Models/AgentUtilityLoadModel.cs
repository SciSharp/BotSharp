namespace BotSharp.Abstraction.Agents.Models;

public class AgentUtilityLoadModel
{
    public string UtilityName { get; set; }
    public UtilityContent Content { get; set; }

    public AgentUtilityLoadModel()
    {
        
    }

    public AgentUtilityLoadModel(string utilityName, UtilityContent content)
    {
        UtilityName = utilityName;
        Content = content;
    }
}


public class UtilityContent
{
    public IEnumerable<UtilityFunction> Functions { get; set; } = [];
    public IEnumerable<UtilityTemplate> Templates { get; set; } = [];

    public UtilityContent()
    {
        
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
    public Dictionary<string, object>? Data { get; set; }

    public UtilityTemplate()
    {
        
    }

    public UtilityTemplate(string name, Dictionary<string, object>? data = null)
    {
        Name = name;
        Data = data;
    }
}

public class UtilityBase
{
    public string Name { get; set; }
}