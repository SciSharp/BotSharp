namespace BotSharp.Abstraction.Functions.Models;

public class ParameterPropertyDef : NameDesc
{
    public ParameterPropertyDef(string name, string description, string type = "string") 
        : base(name, description)
    {
        Type = type;
    }

    [JsonPropertyName("required")]
    public bool Required { get; set; }

    /// <summary>
    /// string, number, object
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";
}
