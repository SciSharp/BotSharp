using BotSharp.Abstraction.Functions.Models;
using System.Text.Json;

namespace BotSharp.Plugin.EntityFrameworkCore.Mappers;

public static class FunctionDefMappers
{
    public static Entities.FunctionDef ToEntity(this FunctionDef model)
    {
        return new Entities.FunctionDef
        {
            Name = model.Name,
            Description = model.Description,
            Channels = model.Channels,
            VisibilityExpression = model.VisibilityExpression,
            Impact = model.Impact,
            Parameters = new Entities.FunctionParametersDef
            {
                Type = model.Parameters.Type,
                Properties = JsonSerializer.Serialize(model.Parameters.Properties),
                Required = model.Parameters.Required,
            }
        };
    }

    public static FunctionDef ToModel(this Entities.FunctionDef model)
    {
        return new FunctionDef
        {
            Name = model.Name,
            Description = model.Description,
            Channels = model.Channels,
            VisibilityExpression = model.VisibilityExpression,
            Impact = model.Impact,
            Parameters = new FunctionParametersDef
            {
                Type = model.Parameters.Type,
                Properties = JsonDocument.Parse(model.Parameters.Properties),
                Required = model.Parameters.Required,
            }
        };
    }
}
