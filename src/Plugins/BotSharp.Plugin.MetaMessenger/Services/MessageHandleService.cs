using BotSharp.Abstraction.Messaging.Models.RichContent;

namespace BotSharp.Plugin.MetaMessenger.Services;

public class MessageHandleService
{
    private readonly IServiceProvider _services;
    private JsonSerializerOptions _serializerOptions;
    public MessageHandleService(IServiceProvider services)
    {
        _services = services;

        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters =
            {
                new RichContentJsonConverter(),
                new TemplateMessageJsonConverter()
            }
        };
    }

    public async Task Handle(string sender, string agentId, string message)
    {
        
        var setting = _services.GetRequiredService<MetaMessengerSetting>();
        var messenger = _services.GetRequiredService<IMessengerGraphAPI>();
        
        var recipient = JsonSerializer.Serialize(new { Id = sender }, _serializerOptions);

        // Marking seen
        await messenger.SendMessage(setting.ApiVersion, setting.PageId,
            new SendingMessageRequest(setting.PageAccessToken, recipient)
            {
                SenderAction = SenderActionEnum.MarkSeen
            });

        // Typing on
        await messenger.SendMessage(setting.ApiVersion, setting.PageId,
            new SendingMessageRequest(setting.PageAccessToken, recipient)
            {
                SenderAction = SenderActionEnum.TypingOn
            });

        // Go to LLM
        var conv = _services.GetRequiredService<IConversationService>();
        conv.SetConversationId(sender, new List<string>
        {
            "channel=messenger"
        });

        var replies = new List<IRichMessage>();
        var result = await conv.SendMessage(agentId, new RoleDialogModel("user", message), async msg =>
        {
            if (!string.IsNullOrEmpty(msg.Content))
            {
                replies.Add(new TextMessage(msg.Content));
            }

            if (msg.RichContent.Message is GenericTemplateMessage genericTemplate)
            {
                replies.Add(new AttachmentMessage
                {
                    Attachment = new AttachmentBody
                    {
                        Payload = genericTemplate
                    }
                });
            }
            else if (msg.RichContent.Message is CouponTemplateMessage couponTemplate)
            {
                replies.Add(new AttachmentMessage
                {
                    Attachment = new AttachmentBody
                    {
                        Payload = couponTemplate
                    }
                });
            }
            else if (msg.RichContent.Message is not TextMessage)
            {
                replies.Add(msg.RichContent.Message);
            }
        },
        _ => Task.CompletedTask,
        _ => Task.CompletedTask);

        // Response to user
        foreach(var reply in replies)
        {
            await messenger.SendMessage(setting.ApiVersion, setting.PageId,
            new SendingMessageRequest(setting.PageAccessToken, recipient)
            {
                Message = JsonSerializer.Serialize(reply, _serializerOptions)
            });
            Thread.Sleep(500);
        }

        // Typing off
        await messenger.SendMessage(setting.ApiVersion, setting.PageId,
            new SendingMessageRequest(setting.PageAccessToken, recipient)
            {
                SenderAction = SenderActionEnum.TypingOff
            });
    }
}
