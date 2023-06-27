using Microsoft.Extensions.DependencyInjection;
using Senparc.NeuChar.App.AppStore;
using Senparc.NeuChar.Entities;
using Senparc.Weixin.MP.Entities;
using Senparc.Weixin.MP.Entities.Request;
using Senparc.Weixin.MP.MessageContexts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BotSharp.Plugin.WeChat
{
    public class BotSharpMessageHandler : Senparc.Weixin.MP.MessageHandlers.MessageHandler<DefaultMpMessageContext>
    {
        public static Func<Stream, PostModel, int, IServiceProvider, BotSharpMessageHandler> GenerateMessageHandler = (stream, postModel, maxRecordCount, serviceProvider)
                         => new BotSharpMessageHandler(stream, postModel, maxRecordCount, false /* 是否只允许处理加密消息，以提高安全性 */, serviceProvider: serviceProvider);

        public BotSharpMessageHandler(Stream inputStream, PostModel postModel, int maxRecordCount = 0, bool onlyAllowEncryptMessage = false, DeveloperInfo developerInfo = null, IServiceProvider serviceProvider = null) : base(inputStream, postModel, maxRecordCount, onlyAllowEncryptMessage, developerInfo, serviceProvider)
        {
        }

        public BotSharpMessageHandler(XDocument requestDocument, PostModel postModel, int maxRecordCount = 0, bool onlyAllowEncryptMessage = false, DeveloperInfo developerInfo = null, IServiceProvider serviceProvider = null) : base(requestDocument, postModel, maxRecordCount, onlyAllowEncryptMessage, developerInfo, serviceProvider)
        {
        }

        public BotSharpMessageHandler(RequestMessageBase requestMessageBase, PostModel postModel, int maxRecordCount = 0, bool onlyAllowEncryptMessage = false, DeveloperInfo developerInfo = null, IServiceProvider serviceProvider = null) : base(requestMessageBase, postModel, maxRecordCount, onlyAllowEncryptMessage, developerInfo, serviceProvider)
        {
        }

        public override IResponseMessageBase DefaultResponseMessage(IRequestMessageBase requestMessage)
        {
            return null;
        }

        public async override Task<IResponseMessageBase> OnTextRequestAsync(RequestMessageText requestMessage)
        {
            var messageQueue = ServiceProvider.GetRequiredService<IMessageQueue>();
            await messageQueue.EnqueueAsync(new WeChatMessage()
            {
                OpenId = OpenId,
                Message = requestMessage.Content,
                Type = "text"
            });
            return await base.OnTextRequestAsync(requestMessage);
        }
    }
}
