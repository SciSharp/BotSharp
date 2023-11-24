using Refit;

namespace BotSharp.Plugin.MetaMessenger.MessagingModels;

/// <summary>
/// https://developers.facebook.com/docs/messenger-platform/reference/send-api#parameters
/// </summary>
public class SendingMessageRequest
{
    /// <example>
    /// {'id':'6610279455689235'}
    /// </example>
    [AliasAs("recipient")]
    public string Recipient { get; set; }

    /// <example>
    /// {'text':'hello,world'}
    /// </example>
    [AliasAs("message")]
    public string Message { get; set; }

    [AliasAs("access_token")]
    public string AccessToken { get; set; }

    [AliasAs("messaging_type")]
    public string MessagingType { get; set; } = "RESPONSE";

    [AliasAs("sender_action")]
    public SenderActionEnum? SenderAction { get; set; }

    public SendingMessageRequest(string token, string recipient)
    {
        AccessToken = token;
        Recipient = recipient;
    }
}
