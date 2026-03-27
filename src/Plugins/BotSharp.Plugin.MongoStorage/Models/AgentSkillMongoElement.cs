using BotSharp.Abstraction.Agents.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BotSharp.Plugin.MongoStorage.Models;


[BsonIgnoreExtraElements(Inherited = true)]
public class AgentSkillMongoElement
{

    /// <summary>
    /// Name of the Skill
    /// <
    public required string Name { get; set; }

    /// <summary>
    /// Description of the Skill
    /// </summary>
    public required string Description { get; set; }


    public static AgentSkillMongoElement ToMongoElement(AgentSkill skill)
    {
        return new AgentSkillMongoElement
        {
            Name = skill.Name, 
            Description = skill.Description 
        };
    }

    public static AgentSkill ToDomainElement(AgentSkillMongoElement skill)
    {
        return new AgentSkill
        {
            Name = skill.Name,
           Description= skill.Description 
        };
    }
}
