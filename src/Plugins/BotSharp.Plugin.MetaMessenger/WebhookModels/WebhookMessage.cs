namespace BotSharp.Plugin.MetaMessenger.WebhookModels;

public class WebhookMessage
{
    public WebhookMessageUser Sender { get; set; }
    public WebhookMessageUser Recipient { get; set; }
    public WebhookMessageBody Message { get; set; }
    public WebhookMessagePostback Postback { get; set; }
    public long TimeStamp { get; set; }
}
