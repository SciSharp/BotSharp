namespace BotSharp.Plugin.Twilio.Settings;

public class TwilioSetting
{
    /// <summary>
    /// Outbound phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Enable streaming for outbound phone call
    /// </summary>
    public bool StreamingEnabled { get; set; } = false;
    public string AccountSID { get; set; }
    public string AuthToken { get; set; }
    public string AppSID { get; set; }
    public string ApiKeySID { get; set; }
    public string ApiSecret { get; set; }
    public string CallbackHost { get; set; }

    /// <summary>
    /// Default Agent Id to handle inbound phone call
    /// </summary>
    public string? AgentId { get; set; }

    /// <summary>
    /// Human agent phone number if AI can't handle the call
    /// </summary>
    public string? CsrAgentNumber { get; set; }

    public int MaxGatherAttempts { get; set; } = 4;

    public string? MachineDetection { get; set; }

    public bool RecordingEnabled { get; set; } = false;
}
