using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentMcpToolMongoElement
{
    public string Name { get; set; }
    public string ServerId { get; set; }
    public bool Disabled { get; set; }
    public List<McpFunctionMongoElement> Functions { get; set; } = [];

    public static AgentMcpToolMongoElement ToMongoElement(McpTool tool)
    {
        return new AgentMcpToolMongoElement
        {
            Name = tool.Name,
            ServerId = tool.ServerId,
            Disabled = tool.Disabled,
            Functions = tool.Functions?.Select(x => new McpFunctionMongoElement(x.Name))?.ToList() ?? [],
        };
    }

    public static McpTool ToDomainElement(AgentMcpToolMongoElement tool)
    {
        return new McpTool
        {
            Name = tool.Name,
            ServerId = tool.ServerId,
            Disabled = tool.Disabled,
            Functions = tool.Functions?.Select(x => new McpFunction(x.Name))?.ToList() ?? [],
        };
    }
}

[BsonIgnoreExtraElements(Inherited = true)]
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
