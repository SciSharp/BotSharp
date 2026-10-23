using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.Twilio.Models.Relay;

/// <summary>
/// What Twilio posts to the Connect action URL when a ConversationRelay session ends
/// </summary>
public class ConversationRelayActionRequest : ConversationalVoiceRequest
{
    [FromForm]
    public string? SessionId { get; set; }

    /// <summary>
    /// ended (we sent "end"), completed (caller hung up) or failed
    /// </summary>
    [FromForm]
    public string? SessionStatus { get; set; }

    [FromForm]
    public string? SessionDuration { get; set; }

    [FromForm]
    public string? HandoffData { get; set; }

    [FromForm]
    public string? ErrorCode { get; set; }

    [FromForm]
    public string? ErrorMessage { get; set; }

    [FromQuery(Name = "reconnects")]
    public int Reconnects { get; set; }
}
