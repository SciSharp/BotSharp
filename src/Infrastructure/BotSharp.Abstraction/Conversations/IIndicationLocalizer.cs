namespace BotSharp.Abstraction.Conversations;

/// <summary>
/// Puts an indication into the conversation's language before it is pushed. Indications skip
/// TranslationResponseHook -- they go out ahead of the function, where an LLM translation would
/// hold up the very line meant to say "working on it" -- so the host supplies fixed wording instead.
/// Optional: with none registered, indications go out as the function wrote them.
/// </summary>
public interface IIndicationLocalizer
{
    /// <param name="language">The conversation's <c>StateConst.LANGUAGE</c>, e.g. <c>LanguageType.CHINESE</c>.</param>
    /// <returns>The wording for <paramref name="language"/>, or <paramref name="indication"/> unchanged when there is none.</returns>
    string Localize(string indication, string language);
}
