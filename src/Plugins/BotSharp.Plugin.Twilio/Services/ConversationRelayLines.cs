using System.Text.RegularExpressions;

namespace BotSharp.Plugin.Twilio.Services;

/// <summary>
/// What a line the agent said asks, for asking it again
/// </summary>
public static class ConversationRelayLines
{
    // Fewer words than this ("Is that your address?") need the sentence before them to make sense on their own
    private const int ShortQuestionWords = 6;

    private static readonly Regex Sentence = new(@"[^.?!]+[.?!]+", RegexOptions.Compiled);

    // What leads into a question and isn't worth saying again
    private static readonly Regex Acknowledgement = new(@"^(?:(?:Got it|Understood|Okay|Thank you|Thanks|Great|Sure|No problem)[.!]\s*)+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// The last question in the line with, when it is short, the sentence before it ("You're at 100 Main Street. Is that
    /// your address?"), or null when the line asks nothing
    /// </summary>
    public static string? LastQuestion(string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        var sentences = Sentence.Matches(line).Select(m => m.Value.Trim()).Where(s => s.Length > 0).ToList();
        var last = sentences.FindLastIndex(s => s.EndsWith('?'));
        if (last < 0)
        {
            return null;
        }

        var question = sentences[last];
        if (last > 0 && question.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length < ShortQuestionWords
            && !Acknowledgement.IsMatch(sentences[last - 1]))
        {
            question = $"{sentences[last - 1]} {question}";
        }
        // "And is the number ... ?" led on from the sentence before it; asked on its own it starts afresh
        if (question.StartsWith("And ", StringComparison.Ordinal) && question.Length > 4)
        {
            question = char.ToUpperInvariant(question[4]) + question[5..];
        }
        return question;
    }
}
