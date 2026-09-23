namespace BotSharp.Plugin.OpenAI.Providers.Live;

/// <summary>
/// Keeps session.*.append content inside the server's per-append budget.
///
/// The limit is stated in tokens, not characters, and the two diverge sharply by script:
/// Latin prose runs around four characters per token, while CJK is closer to one. A plain
/// character cap is therefore far too generous for Chinese, Japanese and Korean, which is
/// exactly where an over-long append would be rejected.
/// </summary>
internal static class LiveAppendBudget
{
    /// <summary>
    /// Upper bound on how many appends one piece of content may be split across, so a runaway
    /// answer cannot flood the session.
    /// </summary>
    public const int MaxChunks = 5;

    /// <summary>
    /// Marks content that had to be dropped, so the model can tell it saw only part of it.
    /// </summary>
    public const string ElisionMarker = " [...]";

    /// <summary>
    /// Rough token count. Wide (CJK and full-width) characters are charged a whole token each,
    /// everything else a quarter. Deliberately pessimistic: overshooting the limit is rejected
    /// by the server, undershooting only costs a little context.
    /// </summary>
    public static int EstimateTokens(string? text)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        var wide = 0;
        var narrow = 0;

        foreach (var ch in text)
        {
            if (IsWide(ch)) wide++;
            else narrow++;
        }

        return wide + (narrow + 3) / 4;
    }

    /// <summary>
    /// Splits content on sentence boundaries so a long append is delivered in full instead of
    /// being amputated. Appends concatenate on the server, so the pieces reassemble into the
    /// original text - which is why this, and not truncation, is the default treatment.
    /// Returns whether anything still had to be dropped at the chunk ceiling.
    /// </summary>
    public static (List<string> Chunks, bool Truncated) SplitIntoChunks(string text, int maxTokens, int maxChunks)
    {
        if (EstimateTokens(text) <= maxTokens)
        {
            return ([text], false);
        }

        var chunks = new List<string>();
        var remaining = text.AsSpan();

        while (!remaining.IsEmpty && chunks.Count < maxChunks)
        {
            if (EstimateTokens(remaining.ToString()) <= maxTokens)
            {
                chunks.Add(remaining.ToString().Trim());
                remaining = default;
                break;
            }

            var cut = FindCutIndex(remaining, maxTokens);
            var boundary = FindSentenceBoundary(remaining, cut);

            chunks.Add(remaining[..boundary].ToString().Trim());
            remaining = remaining[boundary..].TrimStart();
        }

        chunks.RemoveAll(string.IsNullOrWhiteSpace);
        return (chunks, !remaining.IsEmpty);
    }

    /// <summary>
    /// Largest prefix length that still fits the budget, never landing between a surrogate pair.
    /// </summary>
    private static int FindCutIndex(ReadOnlySpan<char> text, int maxTokens)
    {
        var wide = 0;
        var narrow = 0;

        for (var i = 0; i < text.Length; i++)
        {
            if (IsWide(text[i])) wide++;
            else narrow++;

            if (wide + (narrow + 3) / 4 > maxTokens)
            {
                // Stepping back off a low surrogate keeps the pair intact.
                return char.IsLowSurrogate(text[i]) ? Math.Max(0, i - 1) : i;
            }
        }

        return text.Length;
    }

    /// <summary>
    /// Walks back from the budget limit to the last sentence end, then to the last space,
    /// so a chunk does not stop mid-word.
    /// </summary>
    private static int FindSentenceBoundary(ReadOnlySpan<char> text, int limit)
    {
        // Do not give back more than half the chunk chasing a boundary.
        var floor = limit / 2;

        for (var i = limit - 1; i > floor; i--)
        {
            var ch = text[i];
            if (ch is '.' or '!' or '?' or '\n' or '。' or '！' or '？')
            {
                return i + 1;
            }
        }

        for (var i = limit - 1; i > floor; i--)
        {
            if (char.IsWhiteSpace(text[i]))
            {
                return i + 1;
            }
        }

        return limit;
    }

    private static bool IsWide(char c)
        => (c >= 0x1100 && c <= 0x11FF)     // Hangul Jamo
        || (c >= 0x2E80 && c <= 0xA4CF)     // CJK radicals, kana, CJK unified ideographs
        || (c >= 0xAC00 && c <= 0xD7AF)     // Hangul syllables
        || (c >= 0xF900 && c <= 0xFAFF)     // CJK compatibility ideographs
        || (c >= 0xFF00 && c <= 0xFF60)     // Full-width forms
        || (c >= 0xFFE0 && c <= 0xFFE6);    // Full-width signs
}
