namespace BotSharp.Plugin.Twilio.Settings;

/// <summary>
/// ConversationRelay settings for one agent's calls; each one left out is the shared setting
/// (<see cref="ConversationRelaySetting.For"/>)
/// </summary>
public class ConversationRelayAgentSetting
{
    public string? TranscriptionProvider { get; set; }
    public string? SpeechModel { get; set; }

    /// <summary>
    /// low, medium or high
    /// </summary>
    public string? InterruptSensitivity { get; set; }

    public bool? IgnoreBackchannel { get; set; }
    public int? IdleTimeoutSeconds { get; set; }
    public int? MaxIdlePrompts { get; set; }
    public List<double>? IdleTimeouts { get; set; }
    public bool? IdleAsksAgain { get; set; }
    public double? ResumeAfterCutInSeconds { get; set; }
    public bool? DropBackchannelTails { get; set; }
}
