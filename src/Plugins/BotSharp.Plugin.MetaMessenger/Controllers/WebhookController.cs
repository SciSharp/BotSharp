using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Conversations;
using BotSharp.Plugin.MetaMessenger.GraphAPIs;
using BotSharp.Plugin.MetaMessenger.MessagingModels;
using BotSharp.Plugin.MetaMessenger.Settings;
using BotSharp.Plugin.MetaMessenger.WebhookModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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

    public WebhookController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpGet("/messenger/webhook/{agentId}")]
    public string Verificate([FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string token,
        [FromQuery(Name = "hub.challenge")] string challenge,
        [FromRoute] string agentId)
    {
        Console.WriteLine(agentId);
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
            // received message
            if (req.Entry[0].Messaging[0].Message != null)
            {
                var conv = _services.GetRequiredService<IConversationService>();

                string content = "";
                var senderId = req.Entry[0].Messaging[0].Sender.Id;
                var input = req.Entry[0].Messaging[0].Message.Text;

                var setting = _services.GetRequiredService<MetaMessengerSetting>();
                var messenger = _services.GetRequiredService<IMessengerGraphAPI>();
                var jsonOpt = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };

                // Marking seen
                await messenger.SendMessage(setting.ApiVersion, setting.PageId, new SendingMessageRequest
                {
                    AccessToken = setting.PageAccessToken,
                    Recipient = JsonSerializer.Serialize(new { Id = senderId }, jsonOpt),
                    SenderAction = SenderActionEnum.MarkSeen
                });

                // Typing on
                await messenger.SendMessage(setting.ApiVersion, setting.PageId, new SendingMessageRequest
                {
                    AccessToken = setting.PageAccessToken,
                    Recipient = JsonSerializer.Serialize(new { Id = senderId }, jsonOpt),
                    SenderAction = SenderActionEnum.TypingOn
                });

                // Go to LLM
                var result = await conv.SendMessage(agentId, senderId, new RoleDialogModel("user", input), async msg =>
                {
                    content = msg.Content;
                }, async fn =>
                {
                    /*await messenger.SendMessage(setting.ApiVersion, setting.PageId, new SendingMessageRequest
                    {
                        AccessToken = setting.PageAccessToken,
                        Recipient = JsonSerializer.Serialize(new { Id = sessionId }, jsonOpt),
                        Message = JsonSerializer.Serialize(new { Text = "I'm pulling the relevent information, please wait a second ..." }, jsonOpt)
                    });*/

                    await messenger.SendMessage(setting.ApiVersion, setting.PageId, new SendingMessageRequest
                    {
                        AccessToken = setting.PageAccessToken,
                        Recipient = JsonSerializer.Serialize(new { Id = senderId }, jsonOpt),
                        SenderAction = SenderActionEnum.TypingOn
                    });
                });

                // Response to user
                await messenger.SendMessage(setting.ApiVersion, setting.PageId, new SendingMessageRequest
                {
                    AccessToken = setting.PageAccessToken,
                    Recipient = JsonSerializer.Serialize(new { Id = senderId }, jsonOpt),
                    Message = JsonSerializer.Serialize(new { Text = content }, jsonOpt)
                });

                // Typing off
                await messenger.SendMessage(setting.ApiVersion, setting.PageId, new SendingMessageRequest
                {
                    AccessToken = setting.PageAccessToken,
                    Recipient = JsonSerializer.Serialize(new { Id = senderId }, jsonOpt),
                    SenderAction = SenderActionEnum.TypingOff
                });
            }
        }
        catch (ApiException ex)
        {
            Console.WriteLine(ex.Content);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        return Ok(new WebhookResponse
        {
            Success = true
        });
    }
}
