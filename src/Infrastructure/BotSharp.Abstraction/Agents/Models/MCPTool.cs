namespace BotSharp.Abstraction.Agents.Models;

public class McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("server_id")]
    public string ServerId { get; set; }

    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    [JsonPropertyName("functions")]
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
