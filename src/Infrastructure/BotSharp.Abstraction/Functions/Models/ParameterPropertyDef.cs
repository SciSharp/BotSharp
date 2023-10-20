using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction.Functions.Models;

public class ParameterPropertyDef : NameDesc
{
    public ParameterPropertyDef(string name, string description, string type = "string") 
        : base(name, description)
    {
        Type = type;
    }

    /// <summary>
    /// string, number, object
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";
}
