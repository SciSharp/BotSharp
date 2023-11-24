using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Refit;

namespace BotSharp.Plugin.MetaMessenger.Controllers;

/// <summary>
/// This controller is used to receive Wehbook from Messenger platform
/// https://developers.facebook.com/docs/graph-api/webhooks/
/// </summary>
[AllowAnonymous]
public class WebhookController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public WebhookController(IServiceProvider services, ILogger<WebhookController> logger)
    {
        _services = services;
        _logger = logger;
    }

    [HttpGet("/messenger/webhook/{agentId}")]
    public string Verificate([FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string token,
        [FromQuery(Name = "hub.challenge")] string challenge,
        [FromRoute] string agentId)
    {
        Console.WriteLine($"Verificate {mode} {token} {challenge} {agentId}");
        return challenge;
    }

    /// <summary>
    /// https://developers.facebook.com/docs/messenger-platform/webhooks
    /// </summary>
    /// <returns></returns>
    [HttpPost("/messenger/webhook/{agentId}")]
    public async Task<ActionResult<WebhookResponse>> Messages([FromRoute] string agentId)
    {
        using var stream = new StreamReader(Request.Body);
        var body = await stream.ReadToEndAsync();
        Console.WriteLine(body);
        var req = JsonSerializer.Deserialize<WebhookRequest>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        // TODO validate request
        // https://developers.facebook.com/docs/messenger-platform/webhooks#verification-requests
        try
        {
            string senderId = "";
            string message = "";

            // received message
            if (req.Entry[0].Messaging[0].Message != null)
            {
                senderId = req.Entry[0].Messaging[0].Sender.Id;
                message = req.Entry[0].Messaging[0].Message.Text;
            }
            else if (req.Entry[0].Messaging[0].Postback != null)
            {
                senderId = req.Entry[0].Messaging[0].Sender.Id;
                message = req.Entry[0].Messaging[0].Postback.Payload;
            }

            var handler = _services.GetRequiredService<MessageHandleService>();

            await handler.Handle(senderId, agentId, message);
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }

        return Ok(new WebhookResponse
        {
            Success = true
        });
    }
}
