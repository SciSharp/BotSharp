namespace BotSharp.Plugin.Twilio.Models;

/// <summary>
/// What a line kept only as a record of what the caller heard was, kept as its dialog's MessageLabel: such lines look
/// alike otherwise (assistant, out of the agent's context), and a flow may treat them differently
/// </summary>
public static class ConversationRelayLabels
{
    /// <summary>
    /// Said while the agent worked on a reply ("One moment, please.")
    /// </summary>
    public const string Filler = "twilio_relay_filler";

    /// <summary>
    /// Said to a caller gone quiet: the last question asked again ("Sorry, I didn't catch that. ..."), "Are you still
    /// there?", or "Sorry, can you say it again?". It checks the caller is there, so it is no line of the agent's to say
    /// again when they ask what was said.
    /// </summary>
    public const string IdlePrompt = "twilio_relay_idle_prompt";

    /// <summary>
    /// Said as the session hangs up on a caller who stayed quiet
    /// </summary>
    public const string IdleGoodbye = "twilio_relay_idle_goodbye";

    /// <summary>
    /// Said as the session hangs up on a turn that failed or ran past its time
    /// </summary>
    public const string Apology = "twilio_relay_apology";

    /// <summary>
    /// A line the caller cut into, said again once nothing followed the cut-in (ResumeAfterCutInSeconds)
    /// </summary>
    public const string SaidAgain = "twilio_relay_said_again";
}
