using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Infrastructures.ContentTransmitters;
using BotSharp.Abstraction.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace BotSharp.Plugin.WeChat
{
    public class WeChatBackgroundService : BackgroundService, IMessageQueue
    {
        private readonly Channel<WeChatMessage> _queue;
        private readonly IServiceProvider _service;
        private readonly ILogger<WeChatBackgroundService> _logger;

        public WeChatBackgroundService(
            IServiceProvider service,
            ILogger<WeChatBackgroundService> logger)
        {

            this._service = service;
            this._logger = logger;
            this._queue = Channel.CreateUnbounded<WeChatMessage>();
        }

        private async Task HandleTextMessageAsync(string openid, string message)
        {
            var scoped = _service.CreateScope().ServiceProvider;
            var conversationService = scoped.GetRequiredService<IConversationService>();
            var contentTransfer = scoped.GetRequiredService<IContentTransfer>();

            var conversations = conversationService.GetDialogHistory(openid);
            conversations.Add(new RoleDialogModel
            {
                Role = "User",
                Text = message,
            });

            var container = new ContentContainer
            {
                Conversations = conversations
            };

            var result = await contentTransfer.Transport(container);

            if (result.IsSuccess)
            {
                var output = container.Output.Text.Trim();
                await ReplyTextMessageAsync(openid, output);
                conversationService.AddDialog(new RoleDialogModel()
                {
                    Role = "Assistant",
                    Text = output,
                });
            }
        }

        private async Task ReplyTextMessageAsync(string openid, string content)
        {
            var appId = Senparc.Weixin.Config.SenparcWeixinSetting.WeixinAppId;
            await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(appId, openid, content);
        }

        public async Task EnqueueAsync(WeChatMessage message)
        {
            await _queue.Writer.WriteAsync(message);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = await _queue.Reader.ReadAsync(cancellationToken);
                    await HandleTextMessageAsync(message.OpenId, message.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred Handle Message");
                }
            }
        }
    }
}
