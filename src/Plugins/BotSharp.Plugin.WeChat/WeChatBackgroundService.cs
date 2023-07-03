using BotSharp.Abstraction.Agents;
using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Senparc.Weixin.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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
        private string WeChatAppId => Senparc.Weixin.Config.SenparcWeixinSetting.WeixinAppId;
        public static string AgentId { get; set; }

        public WeChatBackgroundService(
            IServiceProvider service,
            ILogger<WeChatBackgroundService> logger)
        {

            _service = service;
            _logger = logger;
            _queue = Channel.CreateUnbounded<WeChatMessage>();
        }

        private async Task HandleTextMessageAsync(string openid, string message)
        {
            var scoped = _service.CreateScope().ServiceProvider;
            var context = scoped.GetService<IHttpContextAccessor>();
            context.HttpContext = new DefaultHttpContext();
            context.HttpContext.User = new ClaimsPrincipal(
            new ClaimsIdentity(new List<Claim>() { new Claim(ClaimTypes.NameIdentifier, openid) }));


            var conversationService = scoped.GetRequiredService<IConversationService>();

            var latestConversationId = (await conversationService.GetConversations()).OrderByDescending(_ => _.CreatedTime).FirstOrDefault()?.Id;

            latestConversationId ??= (await conversationService.NewConversation(new Conversation()
                {
                    UserId = openid,
                    AgentId = AgentId
            }))?.Id;

            var result = await conversationService.SendMessage(AgentId, latestConversationId, new RoleDialogModel
            {
                Role = "user",
                Text = message,
            });

            await ReplyTextMessageAsync(openid, result);
        }

        private async Task ReplyTextMessageAsync(string openid, string content)
        {
            await Senparc.Weixin.MP.AdvancedAPIs.CustomApi.SendTextAsync(WeChatAppId, openid, content);
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
