using System.Text.Json.Serialization;

namespace BotSharp.Plugin.SqlDriver.Models;

public class SqlReturn
{
    [JsonPropertyName("name")]
    public string Name {  get; set; }

    [JsonPropertyName("alias")]
    public string Alias { get; set; }

    public override string ToString()
    {
        return $"{Alias} - {Name}";
    }
}
