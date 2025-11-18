using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class AgentCodeScriptDocument : MongoBase
{
    public string AgentId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string ScriptType { get; set; } = default!;
    public DateTime CreatedTime { get; set; }
    public DateTime UpdatedTime { get; set; }

    public static AgentCodeScriptDocument ToMongoModel(AgentCodeScript script)
    {
        return new AgentCodeScriptDocument
        {
            Id = script.Id,
            AgentId = script.AgentId,
            Name = script.Name,
            Content = script.Content,
            ScriptType = script.ScriptType
        };
    }

    public static AgentCodeScript ToDomainModel(AgentCodeScriptDocument script)
    {
        return new AgentCodeScript
        {
            Id = script.Id,
            AgentId = script.AgentId,
            Name = script.Name,
            Content = script.Content,
            ScriptType = script.ScriptType,
            CreatedTime = script.CreatedTime,
            UpdatedTime = script.UpdatedTime
        };
    }
}
