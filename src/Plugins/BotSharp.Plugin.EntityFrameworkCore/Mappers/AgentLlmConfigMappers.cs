using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.EntityFrameworkCore.Mappers;

public static class AgentLlmConfigMappers
{
    public static Entities.AgentLlmConfig ToEntity(this AgentLlmConfig model)
    {
        return new Entities.AgentLlmConfig
        {
            Provider = model.Provider,
            Model = model.Model,
            IsInherit = model.IsInherit,
            MaxRecursionDepth = model.MaxRecursionDepth,
        };
    }

    public static AgentLlmConfig ToModel(this Entities.AgentLlmConfig model)
    {
        return new AgentLlmConfig
        {
            Provider = model.Provider,
            Model = model.Model,
            IsInherit = model.IsInherit,
            MaxRecursionDepth = model.MaxRecursionDepth,
        };
    }
}
