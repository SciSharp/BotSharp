using BotSharp.Abstraction.Messaging;
using BotSharp.Abstraction.Messaging.Models.RichContent;

namespace BotSharp.Core.Translation;

public class TranslationService : ITranslationService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TranslationService> _logger;

    public TranslationService(IServiceProvider services,
        ILogger<TranslationService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public T Translate<T>(T data, string language) where T : RichContent<IRichMessage>
    {
        

        return data;
    }
}
