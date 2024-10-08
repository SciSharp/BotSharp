using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.EntityFrameworkCore.Mappers;

public static class AgentTemplateMappers
{

    public static Entities.AgentTemplate ToEntity(this AgentTemplate model)
    {
        return new Entities.AgentTemplate
        {
            Name = model.Name,
            Content = model.Content
        };
    }

    public static AgentTemplate ToModel(this Entities.AgentTemplate model)
    {
        return new AgentTemplate
        {
            Name = model.Name,
            Content = model.Content
        };
    }
}
