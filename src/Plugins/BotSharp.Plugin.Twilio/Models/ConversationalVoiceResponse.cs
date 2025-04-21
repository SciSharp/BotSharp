namespace BotSharp.Plugin.Twilio.Models;

public class ConversationalVoiceResponse
{
    public string AgentId { get; set; } = null!;
    public string ConversationId { get; set; } = null!;
    public List<string> SpeechPaths { get; set; } = [];
    public string? Text { get; set; }
    public string CallbackPath { get; set; } 
    public bool ActionOnEmptyResult { get; set; }

    public string Hints { get; set; }

    /// <summary>
    /// The Phone Number to transfer to
    /// </summary>
    public string? TransferTo { get; set; }
}
