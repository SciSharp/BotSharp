namespace BotSharp.Plugin.MetaMessenger.WebhookModels;

public class WebhookObject
{
    public string Id { get; set; }
    public long Time { get; set; }
    public List<WebhookMessage> Messaging { get; set; }
}
