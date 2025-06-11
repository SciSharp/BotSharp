using BotSharp.Abstraction.Agents.Models;

namespace BotSharp.Plugin.MongoStorage.Models;

[BsonIgnoreExtraElements(Inherited = true)]
public class AgentUtilityMongoElement
{
    public string Category { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool Disabled { get; set; }
    public string? VisibilityExpression { get; set; }
    public List<AgentUtilityItemMongoElement> Items { get; set; } = [];

    public static AgentUtilityMongoElement ToMongoElement(AgentUtility utility)
    {
        return new AgentUtilityMongoElement
        {
            Category = utility.Category,
            Name = utility.Name,
            Disabled = utility.Disabled,
            VisibilityExpression = utility.VisibilityExpression,
            Items = utility.Items?.Select(x => new AgentUtilityItemMongoElement
            {
                FunctionName = x.FunctionName,
                TemplateName = x.TemplateName,
                VisibilityExpression = x.VisibilityExpression
            })?.ToList() ?? []
        };
    }

    public static AgentUtility ToDomainElement(AgentUtilityMongoElement utility)
    {
        return new AgentUtility
        {
            Category = utility.Category,
            Name = utility.Name,
            Disabled = utility.Disabled,
            VisibilityExpression = utility.VisibilityExpression,
            Items = utility.Items?.Select(x => new UtilityItem
            {
                FunctionName = x.FunctionName,
                TemplateName = x.TemplateName,
                VisibilityExpression = x.VisibilityExpression
            })?.ToList() ?? [],
        };
    }
}

public class AgentUtilityItemMongoElement
{
    public string FunctionName { get; set; }
    public string? TemplateName { get; set; }
    public string? VisibilityExpression { get; set; }
}