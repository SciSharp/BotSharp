using BotSharp.Abstraction.MessageHub.Observers;

namespace BotSharp.Core.MessageHub.Observers;

public class ConversationObserver : BotSharpObserverBase<HubObserveData<RoleDialogModel>>
{
    private readonly ILogger<ConversationObserver> _logger;
    private readonly IServiceProvider _services;

    public ConversationObserver(
        IServiceProvider services,
        ILogger<ConversationObserver> logger) : base()
    {
        _services = services;
        _logger = logger;
    }

    public override string Name => nameof(ConversationObserver);

    public override void OnCompleted()
    {
        _logger.LogWarning($"{nameof(ConversationObserver)} receives complete notification.");
    }

    public override void OnError(Exception error)
    {
        _logger.LogError(error, $"{nameof(ConversationObserver)} receives error notification: {error.Message}");
    }

    public override void OnNext(HubObserveData<RoleDialogModel> value)
    {
        var conv = _services.GetRequiredService<IConversationService>();

        if (value.EventName == ChatEvent.OnIndicationReceived)
        {
#if DEBUG
            _logger.LogCritical($"Receiving {value.EventName} ({value.Data.Indication}) in {nameof(ConversationObserver)} - {conv.ConversationId}");
#endif
            //progress.OnFunctionExecuting(value.Data).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
