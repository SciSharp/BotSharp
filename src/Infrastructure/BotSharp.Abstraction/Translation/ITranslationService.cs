namespace BotSharp.Abstraction.Translation;

public interface ITranslationService
{
    Task<T> Translate<T>(Agent router, string messageId, T data, string language = "Spanish", bool clone = true) where T : class;
}
