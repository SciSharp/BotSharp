using BotSharp.Core.Agents;
using BotSharp.Core.Engines;
using EntityFrameworkCore.BootKit;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotSharp.Core.Intents
{
    public static class IntentDriver
    {
        public static Intent GetIntent(this IBotEngine bot, Database dc, string intentId)
        {
            var intent = dc.Table<Intent>()
                .Include(x => x.Contexts)
                .Include(x => x.Responses).ThenInclude(x => x.Contexts)
                .Include(x => x.Responses).ThenInclude(x => x.Parameters).ThenInclude(x => x.Prompts)
                .Include(x => x.Responses).ThenInclude(x => x.Messages)
                .Include(x => x.UserSays).ThenInclude(x => x.Data)
                .FirstOrDefault(x => x.Id == intentId);

            // order parts by time
            intent.UserSays.ForEach(x => x.Data = x.Data.OrderBy(d => d.UpdatedTime).ToList());

            return intent;
        }

        public static String CreateIntent(this Agent agent, Database dc, Intent intent)
        {
            if (dc.Table<Intent>().Any(x => x.Id == intent.Id)) return intent.Id;

            dc.Table<Intent>().Add(intent);

            return intent.Id;
        }
    }
}
