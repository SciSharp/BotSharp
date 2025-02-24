namespace BotSharp.Abstraction.Agents.Models;

public class AgentCP
{
    public string Name { get; set; }

    public string ServerId { get; set; }

    public bool Disabled { get; set; }

    public IEnumerable<ACPFunction> Functions { get; set; } = [];
    public IEnumerable<ACPTemplate> Templates { get; set; } = [];

    public AgentCP()
    {
        
    }

    public AgentCP(
        string name,
        IEnumerable<ACPFunction>? functions = null,
        IEnumerable<ACPTemplate>? templates = null)
    {
        Name = name;
        Functions = functions ?? [];
        Templates = templates ?? [];
    }

    public override string ToString()
    {
        return Name;
    }
}


public class ACPFunction : MCPBase
{
    public ACPFunction()
    {
        
    }

    public ACPFunction(string name)
    {
        Name = name;
    }
}

public class ACPTemplate : MCPBase
{
    public ACPTemplate()
    {
        
    }

    public ACPTemplate(string name)
    {
        Name = name;
    }
}

public class MCPBase
{
    public string Name { get; set; }
}