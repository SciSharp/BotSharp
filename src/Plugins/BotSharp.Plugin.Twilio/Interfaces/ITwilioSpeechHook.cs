using BotSharp.Abstraction.Hooks;

namespace BotSharp.Plugin.Twilio.Interfaces;

/// <summary>
/// Changes what the agent says as it goes out to the voice, without changing the reply itself: the conversation history,
/// and so the model's next turn, keep the words the model wrote.
/// </summary>
/// <remarks>
/// Why: the text-to-speech voice reads "B16649128" as one big number and "Ln" as two letters. Putting the spoken forms in
/// the prompt made the model copy them into its replies, and into the history it reads again.
/// <para>
/// A ConversationRelay session calls this for the agent's reply and its welcome greeting (the opening line), and for a
/// question it asks again when the caller is silent; not for the session's own prompts or a flow's lines. A streamed reply
/// is passed a clause at a time (up to punctuation and a space), so a hook sees whole words and may match across several.
/// </para>
/// <para>
/// Set <see cref="IHookBase.SelfId"/>: a hook with none applies to every agent, and every agent's streamed replies are then
/// held to the end of each clause.
/// </para>
/// </remarks>
public interface ITwilioSpeechHook : IHookBase
{
    /// <summary>
    /// The text as the voice should say it, or <paramref name="text"/> when there is nothing to change. A hook that throws
    /// changes nothing: the caller hears the model's words.
    /// </summary>
    /// <remarks>
    /// Must be idempotent: it is called again on text it already formatted, to match what Twilio says it played (the
    /// voice's words) with what the agent wrote. Must be quick, as it may run under the session's lock.
    /// </remarks>
    string FormatSpeech(string text) => text;
}
