using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.Twilio.Models;

public class ConversationalVoiceRequest : VoiceRequest
{
    [FromQuery(Name = "agent-id")]
    public string AgentId { get; set; }

    [FromRoute]
    public string ConversationId { get; set; }

    [FromRoute]
    public int SeqNum { get; set; }

    public int Attempts { get; set; } = 1;

    public string Intent { get; set; }

    public List<string> States { get; set; } = [];
}
