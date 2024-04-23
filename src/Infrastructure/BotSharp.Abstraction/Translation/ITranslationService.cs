namespace BotSharp.Abstraction.Translation;

public interface ITranslationService
{
    T Translate<T>(T data, string language = "Spanish", bool clone = true) where T : class;
}
