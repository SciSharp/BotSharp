using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.RabbitMQ.Controllers;

[Authorize]
[ApiController]
public class RabbitMQController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly IMQService _mqService;
    private readonly ILogger<RabbitMQController> _logger;

    public RabbitMQController(
        IServiceProvider services,
        IMQService mqService,
        ILogger<RabbitMQController> logger)
    {
        _services = services;
        _mqService = mqService;
        _logger = logger;
    }

    /// <summary>
    /// Publish a scheduled message to be delivered after a delay
    /// </summary>
    /// <param name="request">The scheduled message request</param>
    [HttpPost("/message-queue/publish")]
    public async Task<IActionResult> PublishScheduledMessage([FromBody] PublishScheduledMessageRequest request)
    {
        if (request == null)
        {
            return BadRequest(new PublishMessageResponse { Success = false, Error = "Request body is required." });
        }

        try
        {
            var payload = new ScheduledMessagePayload
            {
                Name = request.Name ?? "Hello"
            };

            var success = await _mqService.PublishAsync(
                payload,
                options: new()
                {
                    TopicName = "my.exchange",
                    RoutingKey = "my.routing",
                    DelayMilliseconds = request.DelayMilliseconds ?? 10000,
                    MessageId = request.MessageId
                });
            return Ok(new { Success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish scheduled message");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new PublishMessageResponse { Success = false, Error = ex.Message });
        }
    }

    /// <summary>
    /// Unsubscribe a consumer
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("/message-queue/unsubscribe/consumer")]
    public async Task<IActionResult> UnSubscribeConsuer([FromBody] UnsubscribeConsumerRequest request)
    {
        if (request == null)
        {
            return BadRequest(new { Success = false, Error = "Request body is required." });
        }

        try
        {
            var success = await _mqService.UnsubscribeAsync(request.Name);
            return Ok(new { Success = success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Failed to unsubscribe consumer {request.Name}");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { Success = false, Error = ex.Message });
        }
    }
}

