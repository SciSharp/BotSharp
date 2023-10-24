namespace BotSharp.Abstraction.Routing.Models;

public class RoutingRule
{
    [JsonIgnore]
    public string AgentId { get; set; }

    [JsonIgnore]
    public string AgentName { get; set; }

    public string Field { get; set; }
    public string Description { get; set; }
    /// <summary>
    /// Field type: string, number, object
    /// </summary>
    public string Type { get; set; } = "string";

    public bool Required { get; set; }

    public string? RedirectTo { get; set; }

    public override string ToString()
    {
        return $"{AgentName} {Field}";
    }

    public RoutingRule()
    {
        
    }
}
