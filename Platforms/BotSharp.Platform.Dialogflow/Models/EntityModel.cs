using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Dialogflow.Models
{
    public class EntityModel : EntityBase
    {
        public string Agent { get; set; }

        public string EntityName { get; set; }

        public string Type { get; set; }

        public string UiColor { get; set; }
    }
}
