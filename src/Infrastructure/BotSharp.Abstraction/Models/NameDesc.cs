namespace BotSharp.Abstraction.Models;

public class NameDesc
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    public NameDesc(string name, string description) 
    { 
        Name = name;
        Description = description;
    }

    public override string ToString()
        => $"{Name}: {Description}";
}
