using BotSharp.Abstraction.Hooks;

namespace BotSharp.Plugin.Twilio.Interfaces;

/// <summary>
/// Changes what the agent's reply sounds like as it goes out to the voice, without changing the reply itself: the
/// conversation history, and so the model's next turn, keep the words the model wrote.
/// </summary>
/// <remarks>
/// Why: the text-to-speech voice reads "B16649128" as one big number and "Ln" as two letters. Putting the spoken forms in
/// the prompt made the model copy them into its replies, and into the history it reads again. A ConversationRelay session
/// calls this for the agent's own reply only (not the session's prompts or a flow's lines), a clause at a time when the
/// reply is streamed, so a hook sees whole words and may match across several of them.
/// </remarks>
public interface ITwilioSpeechHook : IHookBase
{
    /// <summary>
    /// The text as the voice should say it. Should return <paramref name="text"/> when it has nothing to change; a hook
    /// that throws changes nothing (the caller hears the model's words).
    /// </summary>
    string FormatSpeech(string text) => text;
}
