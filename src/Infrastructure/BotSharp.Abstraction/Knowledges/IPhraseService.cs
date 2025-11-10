namespace BotSharp.Abstraction.Knowledges;

public interface IPhraseService
{
    /// <summary>
    /// Search similar phrases in the collection
    /// </summary>
    /// <param name="collection"></param>
    /// <param name="term"></param>
    /// <returns></returns>
    Task<Dictionary<string, float>> SearchPhrasesAsync(string collection, string term);
}
