namespace BotSharp.Abstraction.Agents.Models;

public class AgentUtility
{
    public string Category { get; set; }
    public string Name { get; set; }
    public bool Disabled { get; set; }

    [JsonPropertyName("visibility_expression")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VisibilityExpression { get; set; }

    public IEnumerable<UtilityItem> Items { get; set; } = [];

    public AgentUtility()
    {
        
    }

    public override string ToString()
    {
        return $"{Category}-{Name}";
    }
}

public class UtilityItem
{
    [JsonPropertyName("function_name")]
    public string FunctionName { get; set; } = string.Empty;

    [JsonPropertyName("template_name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TemplateName { get; set; }

    [JsonPropertyName("visibility_expression")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VisibilityExpression { get; set; }
}

//public class UtilityFunction : UtilityBase
//{
//    public UtilityFunction()
//    {
        
//    }

//    public UtilityFunction(string name)
//    {
//        Name = name;
//    }
//}

//public class UtilityTemplate : UtilityBase
//{
//    public UtilityTemplate()
//    {
        
//    }

//    public UtilityTemplate(string name)
//    {
//        Name = name;
//    }
//}

//public class UtilityBase
//{
//    public string Name { get; set; }
//}