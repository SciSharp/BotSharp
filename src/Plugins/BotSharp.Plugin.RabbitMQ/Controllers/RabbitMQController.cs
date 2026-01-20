using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.RabbitMQ.Controllers;

/// <summary>
/// Controller for publishing delayed messages to the message queue
/// </summary>
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
    /// <returns>Publish result with message ID and expected delivery time</returns>
    [HttpPost("/message-queue/scheduled")]
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
                    Exchange = "scheduled.exchange",
                    RoutingKey = "scheduled.routing",
                    MilliSeconds = request.DelayMilliseconds ?? 10000,
                    MessageId = request.MessageId
                });
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish scheduled message");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new PublishMessageResponse { Success = false, Error = ex.Message });
        }
    }
}

