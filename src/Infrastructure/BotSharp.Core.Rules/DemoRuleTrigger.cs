namespace BotSharp.Core.Rules;

public class DemoRuleTrigger : IRuleTrigger
{
    public string Channel => "crontab";
    public string Name => nameof(DemoRuleTrigger);

    public string EntityType { get; set; } = "DemoType";
    public string EntityId { get; set; } = "DemoId";
}
