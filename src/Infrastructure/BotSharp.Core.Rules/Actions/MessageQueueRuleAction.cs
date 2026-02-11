using BotSharp.Core.Rules.Models;

namespace BotSharp.Core.Rules.Actions;

public sealed class MessageQueueRuleAction : IRuleAction
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MessageQueueRuleAction> _logger;

    public MessageQueueRuleAction(
        IServiceProvider services,
        ILogger<MessageQueueRuleAction> logger)
    {
        _services = services;
        _logger = logger;
    }

    public string Name => "BotSharp-message-queue";

    public async Task<RuleActionResult> ExecuteAsync(
        Agent agent,
        IRuleTrigger trigger,
        RuleActionContext context)
    {
        try
        {
            // Get message queue service
            var mqService = _services.GetService<IMQService>();
            if (mqService == null)
            {
                var errorMsg = "Message queue service is not configured. Please ensure a message queue provider (e.g., RabbitMQ) is registered.";
                _logger.LogWarning(errorMsg);
                return RuleActionResult.Failed(errorMsg);
            }

            // Create message payload
            var payload = new RuleMessagePayload
            {
                AgentId = agent.Id,
                TriggerName = trigger.Name,
                Channel = trigger.Channel,
                Text = context.Text,
                Timestamp = DateTime.UtcNow,
                States = context.Parameters
            };

            // Publish message to queue
            var mqOptions = BuildMQPublishOptions(context);
            var success = await mqService.PublishAsync(payload, mqOptions);

            if (success)
            {
                _logger.LogInformation("MessageQueue rule action executed successfully for agent {AgentId}", agent.Id);
                return new RuleActionResult
                {
                    Success = true,
                    Response = $"Message published to queue: {mqOptions.TopicName}-{mqOptions.RoutingKey}"
                };
            }
            else
            {
                var errorMsg = $"Failed to publish message to queue {mqOptions.TopicName}-{mqOptions.RoutingKey}";
                _logger.LogWarning(errorMsg);
                return RuleActionResult.Failed(errorMsg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MessageQueue rule action for agent {AgentId} and trigger {TriggerName}",
                agent.Id, trigger.Name);
            return RuleActionResult.Failed(ex.Message);
        }
    }

    private MQPublishOptions BuildMQPublishOptions(RuleActionContext context)
    {
        var topicName = context.Parameters.TryGetValueOrDefault("mq_topic_name", string.Empty);
        var routingKey = context.Parameters.TryGetValueOrDefault("mq_routing_key", string.Empty);
        var delayMilliseconds = ParseDelay(context);

        return new MQPublishOptions
        {
            TopicName = topicName,
            RoutingKey = routingKey,
            DelayMilliseconds = delayMilliseconds,
            JsonOptions = context.JsonOptions
        };
    }

    private long ParseDelay(RuleActionContext context)
    {
        var qty = (double)context.Parameters.TryGetValueOrDefault("mq_delay_qty", 0M);
        if (qty == 0)
        {
            qty = context.Parameters.TryGetValueOrDefault("mq_delay_qty", 0.0);
        }

        if (qty <= 0)
        {
            return 0L;
        }

        var unit = context.Parameters.TryGetValueOrDefault("mq_delay_unit", string.Empty) ?? string.Empty;
        unit = unit.ToLower();

        var milliseconds = 0L;
        switch (unit)
        {
            case "second":
            case "seconds":
                milliseconds = (long)TimeSpan.FromSeconds(qty).TotalMilliseconds;
                break;
            case "minute":
            case "minutes":
                milliseconds = (long)TimeSpan.FromMinutes(qty).TotalMilliseconds;
                break;
            case "hour":
            case "hours":
                milliseconds = (long)TimeSpan.FromHours(qty).TotalMilliseconds;
                break;
            case "day":
            case "days":
                milliseconds = (long)TimeSpan.FromDays(qty).TotalMilliseconds;
                break;
        }

        return milliseconds;
    }
}
