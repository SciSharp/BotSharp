using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentLlmConfigMongoElement
{
    public string? Provider { get; set; }
    public string? Model { get; set; }
    public bool IsInherit { get; set; }
    public int MaxRecursionDepth { get; set; }
    public int? MaxOutputTokens { get; set; }

    public static AgentLlmConfigMongoElement? ToMongoElement(AgentLlmConfig? config)
    {
        if (config == null) return null;

        return new AgentLlmConfigMongoElement
        {
            Provider = config.Provider,
            Model = config.Model,
            IsInherit = config.IsInherit,
            MaxRecursionDepth = config.MaxRecursionDepth,
            MaxOutputTokens = config.MaxOutputTokens,
        };
    }

    public static AgentLlmConfig? ToDomainElement(AgentLlmConfigMongoElement? config)
    {
        if (config == null) return null;

        return new AgentLlmConfig
        {
            Provider = config.Provider,
            Model = config.Model,
            IsInherit = config.IsInherit,
            MaxRecursionDepth = config.MaxRecursionDepth,
            MaxOutputTokens = config.MaxOutputTokens,
        };
    }
}
