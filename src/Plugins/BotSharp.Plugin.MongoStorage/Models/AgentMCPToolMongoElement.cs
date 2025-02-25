using BotSharp.Abstraction.Agents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentMCPToolMongoElement
{
    public string Name { get; set; }

    public string ServerId { get; set; }

    public bool Disabled { get; set; }
    public List<McpFunctionMongoElement> Functions { get; set; } = [];
    public List<McpTemplateMongoElement> Templates { get; set; } = [];

    public static AgentMCPToolMongoElement ToMongoElement(MCPTool utility)
    {
        return new AgentMCPToolMongoElement
        {
            Name = utility.Name,
            Disabled = utility.Disabled,
            Functions = utility.Functions?.Select(x => new McpFunctionMongoElement(x.Name))?.ToList() ?? [],
            Templates = utility.Templates?.Select(x => new McpTemplateMongoElement(x.Name))?.ToList() ?? []
        };
    }

    public static MCPTool ToDomainElement(AgentMCPToolMongoElement utility)
    {
        return new MCPTool
        {
            Name = utility.Name,
            Disabled = utility.Disabled,
            Functions = utility.Functions?.Select(x => new MCPFunction(x.Name))?.ToList() ?? [],
            Templates = utility.Templates?.Select(x => new ACPTemplate(x.Name))?.ToList() ?? []
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

public class McpTemplateMongoElement
{
    public string Name { get; set; }

    public McpTemplateMongoElement()
    {

    }

    public McpTemplateMongoElement(string name)
    {
        Name = name;
    }
}

