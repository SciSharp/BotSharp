namespace BotSharp.Abstraction.Plugins.Models;

public class PluginMenuDef(string label, string? link = null, string? icon = null, int weight = 0)
{
    [JsonIgnore]
    public string Id { get; set; } = default!;

    public string Label { get; set; } = label;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; set; } = icon;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; } = link;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public EmbeddingData? EmbeddingInfo { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsHeader {  get; set; }

    [JsonIgnore]
    public int Weight { get; set; } = weight;

    [JsonIgnore]
    public List<string>? Roles { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<PluginMenuDef>? SubMenu {  get; set; }

    public override string ToString()
    {
        return $"{Label} {Link} {Weight}";
    }
}

public class EmbeddingData
{
    /// <summary>
    /// Embedding source, e.g., tableau
    /// </summary>
    public string Source { get; set; } = default!;

    /// <summary>
    /// Embedding url
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Url { get; set; }

    /// <summary>
    /// Html tag
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HtmlTag { get; set; }

    /// <summary>
    /// Javascript script src
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ScriptSrc { get; set; }

    /// <summary>
    /// Javascript script type, e.g., module
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ScriptType { get; set; }
}
