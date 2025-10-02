using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Collections;

public class AgentCodeDocument : MongoBase
{
    public string AgentId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Content { get; set; } = default!;

    public static AgentCodeDocument ToMongoModel(AgentCodeScript script)
    {
        return new AgentCodeDocument
        {
            Id = script.Id,
            AgentId = script.AgentId,
            Name = script.Name,
            Content = script.Content
        };
    }

    public static AgentCodeScript ToDomainModel(AgentCodeDocument script)
    {
        return new AgentCodeScript
        {
            Id = script.Id,
            AgentId = script.AgentId,
            Name = script.Name,
            Content = script.Content
        };
    }
}
