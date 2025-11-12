namespace BotSharp.Abstraction.Knowledges;

public interface IPhraseService
{
    Task<List<SearchPhrasesResult>> SearchPhrasesAsync(string term);
}