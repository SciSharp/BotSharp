namespace BotSharp.Plugin.Twilio.Services;

/// <summary>
/// How the history records a reply the caller cut into: the part they heard, then a mark naming the part they didn't
/// </summary>
public static class ConversationRelayCutIn
{
    private const string MarkStart = "[the caller cut in here;";

    /// <summary>
    /// The reply as the history keeps it: <paramref name="heard"/>, then the mark naming <paramref name="unheard"/>
    /// </summary>
    public static string Mark(string heard, string unheard)
    {
        var mark = $"{MarkStart} they did not hear: \"{unheard}\"]";
        return heard.Length == 0 ? mark : $"{heard} {mark}";
    }

    /// <summary>
    /// A stored reply with the end of <paramref name="line"/> after <paramref name="heard"/> marked as not heard, or null
    /// when it can't be: the reply doesn't end on that part, it is marked already, or the caller heard all of it
    /// </summary>
    /// <param name="stored">The reply as the history has it, the line said after its turn at its end</param>
    /// <param name="line">The line that was said, which starts with <paramref name="heard"/></param>
    public static string? MarkStored(string stored, string line, string heard)
    {
        line = line.Trim();
        heard = heard.Trim();
        if (!line.StartsWith(heard, StringComparison.Ordinal) || stored.Contains(MarkStart, StringComparison.Ordinal))
        {
            return null;
        }

        var unheard = line[heard.Length..].Trim();
        var reply = stored.TrimEnd();
        if (unheard.Length == 0 || !reply.EndsWith(unheard, StringComparison.Ordinal))
        {
            return null;
        }

        return Mark(reply[..^unheard.Length].TrimEnd(), unheard);
    }
}
