using BotSharp.Abstraction.Agents.Enums;
using BotSharp.Abstraction.Conversations.Enums;
using BotSharp.Abstraction.Messaging.JsonConverters;
using BotSharp.Abstraction.Messaging.Models.RichContent;
using System.Text.Json.Serialization.Metadata;

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
                new TemplateMessageJsonConverter(),
            },
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { ConditionalSerialization.IgnoreRichType }
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
            $"channel={ConversationChannel.Messenger}"
        });

        var replies = new List<IRichMessage>();
        var result = await conv.SendMessage(agentId,
            new RoleDialogModel(AgentRole.User, message), async msg =>
            {
                if (msg.RichContent != null)
                {
                    // Official API doesn't support to show extra content above the products
                    if (!string.IsNullOrEmpty(msg.RichContent.Message.Text) &&
                        // avoid duplicated text
                        msg.RichContent.Message is not QuickReplyMessage)
                    {
                        replies.Add(new TextMessage(msg.RichContent.Message.Text));
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
                    else if (msg.RichContent.Message is QuickReplyMessage quickReplyMessage)
                    {
                        replies.Add(quickReplyMessage);
                    }
                    else
                    {
                        replies.Add(msg.RichContent.Message);
                    }
                }
                else
                {
                    replies.Add(new TextMessage(msg.Content));
                }
            },
            _ => Task.CompletedTask,
            _ => Task.CompletedTask);

        // Response to user
        foreach(var reply in replies)
        {
            var content = JsonSerializer.Serialize(reply, _serializerOptions);
            await messenger.SendMessage(setting.ApiVersion, setting.PageId,
                new SendingMessageRequest(setting.PageAccessToken, recipient)
                {
                    Message = content
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
