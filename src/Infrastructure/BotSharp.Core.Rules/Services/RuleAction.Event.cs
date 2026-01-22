namespace BotSharp.Core.Rules.Services;

public partial class RuleAction
{
    public async Task<bool> SendEventMessageAsync(RuleDelay delay, RuleEventMessageOptions? options)
    {
        if (options == null || delay == null || delay.Quantity < 0)
        {
            return false;
        }

        var mqService = _services.GetService<IMQService>();
        if (mqService == null)
        {
            return false;
        }

        var ts = delay.Parse();
        if (!ts.HasValue)
        {
            return false;
        }

        _logger.LogWarning($"Start sending delay message {options}");

        var isSent = await mqService.PublishAsync(options.Payload, options: new()
        {
            Exchange = options.Exchange,
            RoutingKey = options.RoutingKey,
            MessageId = options.MessageId,
            MilliSeconds = (long)ts.Value.TotalMilliseconds,
            Arguments = options.Arguments
        });

        _logger.LogWarning($"Complete sending delay message: {(isSent ? "Success" : "Failed")}");
        return isSent;
    }
}
