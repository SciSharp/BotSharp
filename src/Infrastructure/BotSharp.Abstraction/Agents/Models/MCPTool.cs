namespace BotSharp.Abstraction.Agents.Models;

public class MCPTool
{
    public string Name { get; set; }

    public string ServerId { get; set; }

    public bool Disabled { get; set; }

    public IEnumerable<MCPFunction> Functions { get; set; } = [];

    public MCPTool()
    {
        
    }

    public MCPTool(
        string name,
        IEnumerable<MCPFunction>? functions = null)
    {
        Name = name;
        Functions = functions ?? [];
    }

    public override string ToString()
    {
        return Name;
    }
}


public class MCPFunction
{
    public string Name { get; set; }

    public MCPFunction()
    {
        
    } 
}
