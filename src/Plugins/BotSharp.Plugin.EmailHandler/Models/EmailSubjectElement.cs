using BotSharp.Abstraction.Messaging.Models.RichContent.Template;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSharp.Plugin.EmailHandler.Models
{
    public class EmailSubjectElement : GenericElement
    {
        public string Subject { get; set; }
    }
}
