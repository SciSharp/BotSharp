using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Plugin.WeChat
{
    public class WeChatMessage
    {
        public string OpenId { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
    }
}
