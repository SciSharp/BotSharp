using System.Text.Json;

namespace BotSharp.Abstraction.Rules.Models;

public class RuleConfigModel
{
    public string TopologyId { get; set; }
    public string TopologyName { get; set; }
    public JsonDocument CustomParameters { get; set; } = JsonDocument.Parse("{}");
}
