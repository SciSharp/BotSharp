using BotSharp.Abstraction.Agents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentCPMongoElement
{
    public string Name { get; set; }

    public string ServerId { get; set; }

    public bool Disabled { get; set; }
    public List<AcpFunctionMongoElement> Functions { get; set; } = [];
    public List<AcpTemplateMongoElement> Templates { get; set; } = [];

    public static AgentCPMongoElement ToMongoElement(AgentCP utility)
    {
        return new AgentCPMongoElement
        {
            Name = utility.Name,
            Disabled = utility.Disabled,
            Functions = utility.Functions?.Select(x => new AcpFunctionMongoElement(x.Name))?.ToList() ?? [],
            Templates = utility.Templates?.Select(x => new AcpTemplateMongoElement(x.Name))?.ToList() ?? []
        };
    }

    public static AgentCP ToDomainElement(AgentCPMongoElement utility)
    {
        return new AgentCP
        {
            Name = utility.Name,
            Disabled = utility.Disabled,
            Functions = utility.Functions?.Select(x => new ACPFunction(x.Name))?.ToList() ?? [],
            Templates = utility.Templates?.Select(x => new ACPTemplate(x.Name))?.ToList() ?? []
        };
    }
}

public class AcpFunctionMongoElement
{
    public string Name { get; set; }

    public AcpFunctionMongoElement()
    {

    }

    public AcpFunctionMongoElement(string name)
    {
        Name = name;
    }
}

public class AcpTemplateMongoElement
{
    public string Name { get; set; }

    public AcpTemplateMongoElement()
    {

    }

    public AcpTemplateMongoElement(string name)
    {
        Name = name;
    }
}

