namespace BotSharp.Abstraction.Agents.Models;

public class MCPTool
{
    public string ServerId { get; set; }

    public bool Disabled { get; set; }

    public IEnumerable<MCPFunction> Functions { get; set; } = [];

    public MCPTool()
    {
        
    }

    public MCPTool(
        IEnumerable<MCPFunction>? functions = null)
    {
        Functions = functions ?? [];
    }

    public override string ToString()
    {
        return ServerId;
    }
}


public class MCPFunction
{
    public string Name { get; set; }

    public MCPFunction(string name)
    {
        this.Name = name;
    } 
}
