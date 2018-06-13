using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Core.Models
{
    public class NlpToken
    {
        public string Text { get; set; }
        public int Offset { get; set; }
        public int End
        {
            get
            {
                return Offset + Text.Length;
            }
        }
    }
}
