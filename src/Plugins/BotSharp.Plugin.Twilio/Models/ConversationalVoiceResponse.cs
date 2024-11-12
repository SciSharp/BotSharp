namespace BotSharp.Plugin.Twilio.Models;

public class ConversationalVoiceResponse
{
    public List<string> SpeechPaths { get; set; } = [];
    public string CallbackPath { get; set; } 
    public bool ActionOnEmptyResult { get; set; }

    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int Timeout { get; set; } = 3;

    public string Hints { get; set; }
}
