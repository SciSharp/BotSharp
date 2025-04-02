using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.Twilio.Models;

public class ConversationalVoiceRequest : VoiceRequest
{
    [FromQuery(Name = "agent-id")]
    public string AgentId { get; set; } = string.Empty;

    [FromQuery(Name = "conversation-id")]
    public string ConversationId { get; set; } = string.Empty;

    [FromRoute]
    public int SeqNum { get; set; }

    public int Attempts { get; set; } = 1;
    public int AIResponseWaitTime { get; set; } = 0;
    public string? AIResponseErrorMessage { get; set; } = string.Empty;

    public string Intent { get; set; } = string.Empty;

    [FromQuery(Name = "init-audio-file")]
    public string? InitAudioFile { get; set; }

    public List<string> States { get; set; } = [];

    [FromForm]
    public string? CallbackSource { get; set; }

    [FromQuery(Name = "transfer-to")]
    public string? TransferTo { get; set; }

    /// <summary>
    /// machine_start
    /// </summary>
    [FromForm]
    public string? AnsweredBy { get; set; }

    [FromForm]
    public int MachineDetectionDuration { get; set; }

    [FromForm]
    public int CallDuration { get; set; }

    #region Transcription
    [FromForm]
    public string? LanguageCode { get; set; }

    [FromForm]
    public string? Stability { get; set; }

    [FromForm]
    public string? TranscriptionData { get; set; }

    [FromForm]
    public string? Final { get; set; }

    [FromForm]
    public string? Track { get; set; }

    [FromForm]
    public string? SequenceId { get; set; }

    [FromForm]
    public string? TranscriptionEvent { get; set; } 
    #endregion
}