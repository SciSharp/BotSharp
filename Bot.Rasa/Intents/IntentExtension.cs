using Bot.Rasa.Agents;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bot.Rasa.Intents
{
    public static class IntentExtension
    {
        public static string CreateIntent(this Agent agent, Database dc, Intent intent)
        {
            if (dc.Table<Intent>().Any(x => x.Id == intent.Id)) return intent.Id;

            dc.Table<Intent>().Add(intent);

            return intent.Id;
        }
    }
}
