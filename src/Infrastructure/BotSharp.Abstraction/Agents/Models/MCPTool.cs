namespace BotSharp.Abstraction.Agents.Models;

public class McpTool
{
    public string Name { get; set; }
    public string ServerId { get; set; }
    public bool Disabled { get; set; }
    public IEnumerable<McpFunction> Functions { get; set; } = [];

    public McpTool()
    {
        
    }

    public McpTool(
        string name,
        string serverId,
        bool disabled = false,
        IEnumerable<McpFunction>? functions = null)
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


public class McpFunction
{
    public string Name { get; set; }

    public McpFunction(string name)
    {
        Name = name;
    } 
}
