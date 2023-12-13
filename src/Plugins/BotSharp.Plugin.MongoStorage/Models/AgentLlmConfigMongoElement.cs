using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

public class AgentLlmConfigMongoElement
{
    public string? Provider { get; set; }
    public string Model { get; set; }

    public static AgentLlmConfigMongoElement? ToMongoElement(AgentLlmConfig? config)
    {
        if (config == null) return null;

        return new AgentLlmConfigMongoElement
        {
            Provider = config.Provider,
            Model = config.Model
        };
    }

    public static AgentLlmConfig? ToDomainElement(AgentLlmConfigMongoElement? config)
    {
        if (config == null) return null;

        return new AgentLlmConfig
        {
            Provider = config.Provider,
            Model = config.Model
        };
    }
}
