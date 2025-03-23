using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentMCPToolMongoElement
{
    public string Name { get; set; }

    public string ServerId { get; set; }

    public bool Disabled { get; set; }
    public List<McpFunctionMongoElement> Functions { get; set; } = [];

    public static AgentMCPToolMongoElement ToMongoElement(MCPTool utility)
    {
        return new AgentMCPToolMongoElement
        {
            Disabled = utility.Disabled,
            Functions = utility.Functions?.Select(x => new McpFunctionMongoElement(x.Name))?.ToList() ?? [],
        };
    }

    public static MCPTool ToDomainElement(AgentMCPToolMongoElement utility)
    {
        return new MCPTool
        {
            Disabled = utility.Disabled,
            Functions = utility.Functions?.Select(x => new MCPFunction(x.Name))?.ToList() ?? [],
        };
    }
}

public class McpFunctionMongoElement
{
    public string Name { get; set; }

    public McpFunctionMongoElement()
    {

    }

    public McpFunctionMongoElement(string name)
    {
        Name = name;
    }
}
