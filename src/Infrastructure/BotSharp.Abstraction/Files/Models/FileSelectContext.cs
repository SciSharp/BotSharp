namespace BotSharp.Abstraction.Files.Models;

public class FileSelectContext
{
    [JsonPropertyName("selected_ids")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<int>? Selecteds { get; set; }
}
