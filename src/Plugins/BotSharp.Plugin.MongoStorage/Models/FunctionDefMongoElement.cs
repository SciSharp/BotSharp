using BotSharp.Abstraction.Functions.Models;
using System.Text.Json;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class FunctionDefMongoElement
{
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public List<string>? Channels { get; set; }
    public string? VisibilityExpression { get; set; }
    public string? Impact { get; set; }
    public FunctionParametersDefMongoElement Parameters { get; set; } = new();
    public string? Output { get; set; }

    public static FunctionDefMongoElement ToMongoElement(FunctionDef function)
    {
        return new FunctionDefMongoElement
        {
            Name = function.Name,
            Description = function.Description,
            Channels = function.Channels,
            VisibilityExpression = function.VisibilityExpression,
            Impact = function.Impact,
            Parameters = new FunctionParametersDefMongoElement
            {
                Type = function.Parameters.Type,
                Properties = JsonSerializer.Serialize(function.Parameters.Properties),
                Required = function.Parameters.Required,
            },
            Output = function.Output
        };
    }

    public static FunctionDef ToDomainElement(FunctionDefMongoElement function)
    {
        return new FunctionDef
        {
            Name = function.Name,
            Description = function.Description,
            Channels = function.Channels,
            VisibilityExpression = function.VisibilityExpression,
            Impact = function.Impact,
            Parameters = new FunctionParametersDef
            {
                Type = function.Parameters.Type,
                Properties = JsonSerializer.Deserialize<JsonDocument>(function.Parameters.Properties.IfNullOrEmptyAs("{}")),
                Required = function.Parameters.Required,
            },
            Output = function.Output
        };
    }
}

public class FunctionParametersDefMongoElement
{
    public string Type { get; set; } = default!;
    public string Properties { get; set; } = default!;
    public List<string> Required { get; set; } = [];
}
