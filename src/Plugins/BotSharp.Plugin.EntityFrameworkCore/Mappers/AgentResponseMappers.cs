using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.EntityFrameworkCore.Mappers;

public static class AgentResponseMappers
{
    public static Entities.AgentResponse ToEntity(this AgentResponse model)
    {
        return new Entities.AgentResponse
        {
            Prefix = model.Prefix,
            Intent = model.Intent,
            Content = model.Content
        };
    }

    public static AgentResponse ToModel(this Entities.AgentResponse model)
    {
        return new AgentResponse
        {
            Prefix = model.Prefix,
            Intent = model.Intent,
            Content = model.Content
        };
    }
}
