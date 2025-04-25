namespace BotSharp.Plugin.Twilio.Settings;

public class TwilioSetting
{
    /// <summary>
    /// Outbound phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    public string AccountSID { get; set; } = null!;
    public string AppSID { get; set; }
    public string ApiKeySID { get; set; }
    public string ApiSecret { get; set; }
    public string CallbackHost { get; set; } = null!;

    public string SpeechModel { get; set; } = "googlev2_telephony";
    public string? MessagingShortCode { get; set; }

    /// <summary>
    /// Human agent phone number if AI can't handle the call
    /// </summary>
    public string? CsrAgentNumber { get; set; }

    public int MaxGatherAttempts { get; set; } = 10;

    public int GatherTimeout { get; set; } = 1;

    public string? MachineDetection { get; set; }
    public int MachineDetectionSilenceTimeout { get; set; } = 2500;

    public bool RecordingEnabled { get; set; } = false;
    public bool TranscribeEnabled { get; set; } = false;

    public bool GenerateReplyAudio { get; set; } = true;
    public bool GenerateEndingAudio { get; set; } = true;
}
