namespace BotSharp.Abstraction.Agents.Models;

public class AgentMCP
{
    public string Name { get; set; }

    public string ServerId { get; set; }

    public bool Disabled { get; set; }

    public IEnumerable<MCPFunction> Functions { get; set; } = [];
    public IEnumerable<MCPTemplate> Templates { get; set; } = [];

    public AgentMCP()
    {
        
    }

    public AgentMCP(
        string name,
        IEnumerable<MCPFunction>? functions = null,
        IEnumerable<MCPTemplate>? templates = null)
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


public class MCPFunction : MCPBase
{
    public MCPFunction()
    {
        
    }

    public MCPFunction(string name)
    {
        Name = name;
    }
}

public class MCPTemplate : MCPBase
{
    public MCPTemplate()
    {
        
    }

    public MCPTemplate(string name)
    {
        Name = name;
    }
}

public class MCPBase
{
    public string Name { get; set; }
}