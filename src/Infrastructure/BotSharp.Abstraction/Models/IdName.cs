namespace BotSharp.Abstraction.Models;

public class IdName
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    public IdName()
    {
        
    }

    public IdName(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public override string ToString()
    {
        return $"{Id}: {Name}";
    }
}
