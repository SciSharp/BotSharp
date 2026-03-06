using System.Text.Json;

namespace BotSharp.Abstraction.Rules.Models;

public class RuleConfigModel
{
    public string TopologyId { get; set; }
    public string TopologyProvider { get; set; }
    public JsonDocument CustomConfig { get; set; }
}
