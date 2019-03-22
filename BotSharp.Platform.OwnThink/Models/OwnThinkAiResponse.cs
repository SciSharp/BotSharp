using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.OwnThink.Models
{
    public class OwnThinkAiResponse
    {
        public OwnThinkChatResponseData Data { get; set; }

        public string Message { get; set; }
    }

    public class OwnThinkChatResponseData
    {
        public OwnThinkChatResponseDataInfo Info { get; set; }

        public int Type { get; set; }
    }

    public class OwnThinkChatResponseDataInfo
    {
        public string Text { get; set; }
    }
}
