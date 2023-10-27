using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Abstraction.Messaging.Models.RichContent.Template
{
    public class TemplateMessage<T>
    {
        public T Attachment { get; set; }
    }
}
