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
        string serverId,
        bool disabled = false,
        IEnumerable<MCPFunction>? functions = null)
    {
        Name = name;
        ServerId = serverId;
        Disabled = disabled;
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
        Name = name;
    } 
}
