namespace BotSharp.Core.Rules;

public class DemoRuleTrigger : IRuleTrigger
{
    public string Channel => "test";
    public string Name => nameof(DemoRuleTrigger);

    public string EntityType { get; set; }
    public string EntityId { get; set; }
}
