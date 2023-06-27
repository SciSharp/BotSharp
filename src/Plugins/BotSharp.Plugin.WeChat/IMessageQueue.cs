using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.WeChat
{
    public interface IMessageQueue
    {
        Task EnqueueAsync(WeChatMessage message);
    }
}
