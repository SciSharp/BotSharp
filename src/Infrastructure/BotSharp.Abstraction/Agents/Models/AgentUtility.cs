namespace BotSharp.Abstraction.Agents.Models;

public class AgentUtility
{
    public string Name { get; set; }
    public UtilityContent Content { get; set; }

    public AgentUtility()
    {
        
    }

    public AgentUtility(string utilityName, UtilityContent content)
    {
        Name = utilityName;
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