using BotSharp.Platform.Articulate.Models;
using BotSharp.Platform.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.Platform.Articulate.ViewModels
{
    public class IntentViewModel : IntentBase
    {
        public string IntentName { get; set; }

        public string Agent { get; set; }

        public string Domain { get; set; }

        public bool UsePostFormat { get; set; }

        public bool UseWebhook { get; set; }

        public List<IntentExampleModel> Examples { get; set; }
    }
}
