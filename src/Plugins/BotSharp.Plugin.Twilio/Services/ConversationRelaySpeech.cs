namespace BotSharp.Plugin.Twilio.Services;

/// <summary>
/// Where a reply being streamed is cut into the pieces a speech hook formats (<see cref="Interfaces.ITwilioSpeechHook"/>)
/// </summary>
public static class ConversationRelaySpeech
{
    private const string ClauseEnds = ",.;:!?";

    /// <summary>
    /// Splits text into what is ready to be formatted and said, up to the last end of a clause, and what waits for more
    /// of the reply
    /// </summary>
    /// <remarks>
    /// Why: the model's tokens are pieces of words ("B166", "49128"), so a number or an address can't be recognised until
    /// its end has arrived. A clause end is punctuation followed by a space ("Ln, Atlanta"), so "3.5" is not one, and
    /// neither is a comma still waiting for what follows it. Without a clause end nothing is ready.
    /// </remarks>
    public static (string Ready, string Waiting) SplitAtClauseEnd(string text)
    {
        for (var i = text.Length - 1; i >= 0; i--)
        {
            var isEnd = text[i] == '\n'
                || ClauseEnds.Contains(text[i]) && i + 1 < text.Length && char.IsWhiteSpace(text[i + 1]);
            if (isEnd)
            {
                return (text[..(i + 1)], text[(i + 1)..]);
            }
        }
        return (string.Empty, text);
    }
}
