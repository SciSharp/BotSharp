using BotSharp.Plugin.MessageQueue.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BotSharp.Plugin.MessageQueue.Controllers;

/// <summary>
/// Controller for publishing delayed messages to the message queue
/// </summary>
[Authorize]
[ApiController]
public class MessageQueueController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly IMQService _mqService;
    private readonly ILogger<MessageQueueController> _logger;

    public MessageQueueController(
        IServiceProvider services,
        IMQService mqService,
        ILogger<MessageQueueController> logger)
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
                exchange: "scheduled.exchange",
                routingkey: "scheduled.routing",
                milliseconds: request.DelayMilliseconds ?? 10000,
                messageId: request.MessageId ?? Guid.NewGuid().ToString());

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

