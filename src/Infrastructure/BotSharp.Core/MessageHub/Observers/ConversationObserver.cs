namespace BotSharp.Core.MessageHub.Observers;

public class ConversationObserver : IObserver<HubObserveData<RoleDialogModel>>
{
    private readonly ILogger _logger;
    private IServiceProvider _services;

    public ConversationObserver(ILogger logger)
    {
        _logger = logger;
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
        var progress = _services.GetRequiredService<IConversationProgressService>();

        if (value.EventName == ChatEvent.OnIndicationReceived
            && progress.OnFunctionExecuting != null)
        {
#if DEBUG
            _logger.LogCritical($"Receiving {value.EventName} in {nameof(ConversationObserver)}");
#endif
            progress.OnFunctionExecuting(value.Data).ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
