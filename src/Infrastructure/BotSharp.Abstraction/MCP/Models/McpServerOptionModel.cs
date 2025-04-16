namespace BotSharp.Abstraction.MCP.Models;

public class McpServerOptionModel : IdName
{
    public IEnumerable<string> Tools { get; set; } = [];

    public McpServerOptionModel() : base()
    {
        
    }

    public McpServerOptionModel(
        string id,
        string name,
        IEnumerable<string> tools) : base(id, name)
    {
        Tools = tools ?? [];
    }
}
