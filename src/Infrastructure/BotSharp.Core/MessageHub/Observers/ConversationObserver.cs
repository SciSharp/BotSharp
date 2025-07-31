using BotSharp.Abstraction.MessageHub.Observers;

namespace BotSharp.Core.MessageHub.Observers;

public class ConversationObserver : IBotSharpObserver<HubObserveData<RoleDialogModel>>
{
    private readonly ILogger<ConversationObserver> _logger;
    private IServiceProvider _services;
    private bool _isActive;

    public ConversationObserver(
        ILogger<ConversationObserver> logger)
    {
        _logger = logger;
    }

    public bool IsActive => _isActive;

    public void Activate()
    {
        _isActive = true;
    }

    public void Deactivate()
    {
        _isActive = false;
    }

    public void OnCompleted()
    {
        _logger.LogWarning($"{nameof(ConversationObserver)} receives complete notification.");
    }

    public void OnError(Exception error)
    {
        _logger.LogError(error, $"{nameof(ConversationObserver)} receives error notification: {error.Message}");
    }

    public void OnNext(HubObserveData<RoleDialogModel> value)
    {
        _services = value.ServiceProvider;
        //var progress = _services.GetRequiredService<IConversationProgressService>();

        if (value.EventName == ChatEvent.OnIndicationReceived)
        {
#if !DEBUG
            _logger.LogCritical($"Receiving {value.EventName} ({value.Data.Indication}) in {nameof(ConversationObserver)}");
#endif
            //progress.OnFunctionExecuting(value.Data).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
