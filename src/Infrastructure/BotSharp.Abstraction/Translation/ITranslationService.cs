
using BotSharp.Abstraction.Messaging.Models.RichContent;
using BotSharp.Abstraction.Messaging;

namespace BotSharp.Abstraction.Translation;

public interface ITranslationService
{

    T Translate<T>(T data, string language) where T : RichContent<IRichMessage>;
}
