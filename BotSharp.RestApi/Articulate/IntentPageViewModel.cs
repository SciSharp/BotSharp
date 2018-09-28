using BotSharp.Core.Engines.Articulate;
using System;
using System.Collections.Generic;
using System.Text;

namespace BotSharp.RestApi.Articulate
{
    public class IntentPageViewModel
    {
        public List<IntentModel> Intents { get; set; }

        public int Total { get; set; }
    }
}
