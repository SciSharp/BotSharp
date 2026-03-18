using System.Text.Json;

namespace BotSharp.Abstraction.Rules.Options;

public class RuleNodeExecutionOptions
{
    public string Text { get; set; }
    public IEnumerable<MessageState> States { get; set; } = [];
    public JsonSerializerOptions? JsonOptions { get; set; }
    public RuleFlowOptions? Flow { get; set; }
}
